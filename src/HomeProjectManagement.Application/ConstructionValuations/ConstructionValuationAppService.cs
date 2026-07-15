using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.ConstructionValuations;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.ConstructionValuations;

/// <summary>
/// Thin orchestration over the <see cref="ConstructionValuation"/> aggregate: load the catalog it assesses,
/// freeze each row from the catalog item's current estimate + the appraiser's completion %, and commit
/// through the unit of work. Import is idempotent by source content hash (a re-run of the same document
/// returns the existing snapshot rather than duplicating it — the same pattern as BoQ ingestion). Audit
/// fields are stamped inside the unit of work.
/// </summary>
public sealed class ConstructionValuationAppService(
    IConstructionValuationRepository repository,
    IValuationCatalogRepository catalogs,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IConstructionValuationAppService
{
    public async Task<ConstructionValuationDto?> CaptureAsync(
        Guid catalogId,
        CaptureConstructionValuationCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await catalogs.GetAsync(new ValuationCatalogId(catalogId), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        // Idempotent import: the same source document (by content hash) maps to the same snapshot.
        var hash = NormalizeHash(command.SourceContentHash);
        if (hash is not null)
        {
            var already = await repository.GetBySourceContentHashAsync(catalog.Id, hash, cancellationToken);
            if (already is not null)
            {
                return ConstructionValuationMapper.ToDto(already);
            }
        }

        var now = timeProvider.GetUtcNow();

        var valuation = ConstructionValuation.Capture(
            catalog.Id,
            command.AssessedOn,
            new ExchangeRate(
                command.ExchangeRate.BaseCurrency,
                command.ExchangeRate.QuoteCurrency,
                command.ExchangeRate.Rate,
                command.ExchangeRate.AsOf),
            now,
            command.Appraiser,
            ToSourceDocument(command.SourceDocumentFileName, command.SourceDocumentUrl, now),
            hash);

        foreach (var input in command.Items)
        {
            var item = catalog.Items.FirstOrDefault(i => i.Id == new ValuationCatalogItemId(input.ValuationCatalogItemId));
            if (item is null)
            {
                throw new DomainValidationException(
                    "The assessed item does not belong to this valuation catalog.",
                    nameof(input.ValuationCatalogItemId),
                    code: "ValuationCatalogItemNotFound");
            }

            // Retired items are not assessed on new visits — skip rather than fail the whole import.
            if (!item.IsActive)
            {
                continue;
            }

            // Freeze the estimate from the catalog item's current totals; the domain derives the
            // completed/remaining values from these and the completion %, once, then never recomputes.
            valuation.AddAssessedItem(
                item.Id,
                item.Name,
                item.TotalCostWithoutVat,
                item.TotalCostWithVat,
                input.CompletionPercentage);
        }

        repository.Add(valuation);
        await unitOfWork.CommitAsync(cancellationToken);
        return ConstructionValuationMapper.ToDto(valuation);
    }

    public async Task<ConstructionValuationDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var valuation = await repository.GetAsync(new ConstructionValuationId(id), cancellationToken);
        return valuation is null ? null : ConstructionValuationMapper.ToDto(valuation);
    }

    public async Task<IReadOnlyList<ConstructionValuationDto>?> ListByCatalogAsync(
        Guid catalogId,
        CancellationToken cancellationToken = default)
    {
        var catalog = await catalogs.GetAsync(new ValuationCatalogId(catalogId), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        var valuations = await repository.ListByCatalogAsync(catalog.Id, cancellationToken);
        return valuations.Select(ConstructionValuationMapper.ToDto).ToList();
    }

    // Normalise a hex SHA-256 digest the same way the aggregate does, so idempotency comparison is stable.
    private static string? NormalizeHash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    // Build a provenance reference when the agent supplies a source file name. The uploader is the
    // current (authenticated) stakeholder and the upload time is now; the URL falls back to the name.
    private DocumentReference? ToSourceDocument(string? fileName, string? url, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var trimmedName = fileName.Trim();
        var resolvedUrl = string.IsNullOrWhiteSpace(url) ? trimmedName : url.Trim();
        return new DocumentReference(trimmedName, resolvedUrl, now, currentUser.UserId);
    }
}
