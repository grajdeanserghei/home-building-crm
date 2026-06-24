using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.UnitsOfMeasure;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.BillsOfQuantities;

/// <summary>
/// Thin orchestration over the <see cref="BillOfQuantities"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. It enforces at most one BoQ per
/// bid, checks the parent bid exists, and validates that every line item references an <b>active</b>
/// unit of measure before the line reaches the aggregate (the currency and edit-while-open invariants
/// are enforced by the aggregate itself). Audit fields are stamped inside the unit of work.
/// </summary>
public sealed class BillOfQuantitiesAppService(
    IBillOfQuantitiesRepository repository,
    IBidRepository bids,
    IUnitOfMeasureRepository units,
    IContractorRepository contractors,
    IWorkPackageRepository workPackages,
    IBoqSpreadsheetExporter exporter,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBillOfQuantitiesAppService
{
    public async Task<BillOfQuantitiesDto?> GetByBidAsync(
        Guid bidId,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetByBidAsync(new BidId(bidId), cancellationToken);
        return boq is null ? null : ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        return boq is null ? null : ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> DraftAsync(
        Guid bidId,
        DraftBillOfQuantitiesCommand command,
        CancellationToken cancellationToken = default)
    {
        // The owning bid must exist before drafting a BoQ that references it by id.
        var bid = await bids.GetAsync(new BidId(bidId), cancellationToken);
        if (bid is null)
        {
            return null;
        }

        // At most one BoQ per bid. If one already exists, either this is a safe re-run of the same
        // ingestion (same source hash → return it idempotently) or it is a different quote, which must
        // supersede the existing one via ReplaceContentsAsync rather than drafting a second BoQ.
        var existing = await repository.GetByBidAsync(bid.Id, cancellationToken);
        if (existing is not null)
        {
            var hash = NormalizeHash(command.SourceContentHash);
            if (hash is not null && existing.SourceContentHash == hash)
            {
                return ToDto(existing);
            }

            throw new DomainConflictException(
                "A bill of quantities already exists for this bid. Replace its contents instead of drafting another.",
                code: "BoqAlreadyExistsForBid",
                parameters: new Dictionary<string, object?> { ["bidId"] = bidId });
        }

        var now = timeProvider.GetUtcNow();

        var boq = BillOfQuantities.Draft(
            bid.Id,
            command.PricingCurrency,
            now,
            command.Reference,
            ToExchangeRate(command.ExchangeRate),
            command.SubmittedOn,
            command.ValidUntil,
            ToSourceDocument(command.SourceDocumentFileName, command.SourceDocumentUrl, now),
            command.SourceContentHash);

        repository.Add(boq);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> UpdateAsync(
        Guid id,
        UpdateBillOfQuantitiesCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.UpdateDetails(
            command.Reference,
            ToExchangeRate(command.ExchangeRate),
            command.SubmittedOn,
            command.ValidUntil);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> ReplaceContentsAsync(
        Guid id,
        ReplaceBoqContentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        // Re-point the header, then drop the old sections and provenance so the revised deviz can be
        // re-ingested onto the same BoQ. The aggregate guards that the BoQ is still editable.
        boq.UpdateDetails(
            command.Reference,
            ToExchangeRate(command.ExchangeRate),
            command.SubmittedOn,
            command.ValidUntil);

        boq.ReplaceContents(
            ToSourceDocument(command.SourceDocumentFileName, command.SourceDocumentUrl, now),
            command.SourceContentHash,
            now);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> ChangeStatusAsync(
        Guid id,
        ChangeBoqStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.ChangeStatus(command.Status, timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> AddSectionAsync(
        Guid id,
        SectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.AddSection(command.Name, command.Sequence, command.Description);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> UpdateSectionAsync(
        Guid id,
        Guid sectionId,
        SectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.UpdateSection(new SectionId(sectionId), command.Name, command.Sequence, command.Description))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveSectionAsync(Guid id, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.RemoveSection(new SectionId(sectionId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<BillOfQuantitiesDto?> AddLineItemAsync(
        Guid id,
        Guid sectionId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var added = boq.AddLineItem(
            new SectionId(sectionId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            ToVatRate(command.VatRatePercentage),
            command.Sequence,
            command.Notes);

        if (added is null)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<AddBoqLineItemsResult?> AddLineItemsAsync(
        Guid id,
        Guid sectionId,
        IReadOnlyList<BoqLineItemInput> items,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        var section = boq.Sections.FirstOrDefault(s => s.Id == new SectionId(sectionId));
        if (section is null)
        {
            return null;
        }

        // Load the active vocabulary once; each line's free-text token is normalised against it.
        var activeUnits = await units.ListAsync(includeInactive: false, cancellationToken);

        // Append after the section's current lines, keeping a stable order for the batch.
        var nextSequence = section.LineItems.Count == 0 ? 1 : section.LineItems.Max(li => li.Sequence) + 1;

        var unresolved = new List<UnresolvedBoqLine>();
        var addedAny = false;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var unit = activeUnits.FirstOrDefault(u => u.Recognizes(item.Unit));
            if (unit is null)
            {
                // Unresolved units don't fail the batch — flag the offending token and move on.
                unresolved.Add(new UnresolvedBoqLine(index, item.Description, item.Unit));
                continue;
            }

            boq.AddLineItem(
                section.Id,
                item.Description,
                item.Quantity,
                unit.Id,
                ToMoney(item.UnitPrice),
                ToVatRate(item.VatRatePercentage),
                nextSequence++,
                item.Notes);
            addedAny = true;
        }

        if (addedAny)
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return new AddBoqLineItemsResult(ToDto(boq), unresolved);
    }

    public async Task<AddBoqLineItemsResult?> AddSubsectionLineItemsAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        IReadOnlyList<BoqLineItemInput> items,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        var section = boq.Sections.FirstOrDefault(s => s.Id == new SectionId(sectionId));
        var subsection = section?.Subsections.FirstOrDefault(s => s.Id == new SubsectionId(subsectionId));
        if (subsection is null)
        {
            return null;
        }

        // Load the active vocabulary once; each line's free-text token is normalised against it.
        var activeUnits = await units.ListAsync(includeInactive: false, cancellationToken);

        // Append after the subsection's current lines, keeping a stable order for the batch.
        var nextSequence = subsection.LineItems.Count == 0 ? 1 : subsection.LineItems.Max(li => li.Sequence) + 1;

        var unresolved = new List<UnresolvedBoqLine>();
        var addedAny = false;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var unit = activeUnits.FirstOrDefault(u => u.Recognizes(item.Unit));
            if (unit is null)
            {
                // Unresolved units don't fail the batch — flag the offending token and move on.
                unresolved.Add(new UnresolvedBoqLine(index, item.Description, item.Unit));
                continue;
            }

            boq.AddSubsectionLineItem(
                section!.Id,
                subsection.Id,
                item.Description,
                item.Quantity,
                unit.Id,
                ToMoney(item.UnitPrice),
                ToVatRate(item.VatRatePercentage),
                nextSequence++,
                item.Notes);
            addedAny = true;
        }

        if (addedAny)
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return new AddBoqLineItemsResult(ToDto(boq), unresolved);
    }

    public async Task<BillOfQuantitiesDto?> ReviseLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid lineItemId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var revised = boq.ReviseLineItem(
            new SectionId(sectionId),
            new LineItemId(lineItemId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            ToVatRate(command.VatRatePercentage),
            command.Sequence,
            command.Notes);

        if (!revised)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid lineItemId,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.RemoveLineItem(new SectionId(sectionId), new LineItemId(lineItemId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<BillOfQuantitiesDto?> AddSubsectionAsync(
        Guid id,
        Guid sectionId,
        SubsectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || boq.AddSubsection(new SectionId(sectionId), command.Name, command.Sequence, command.Description) is null)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> UpdateSubsectionAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        SubsectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null
            || !boq.UpdateSubsection(new SectionId(sectionId), new SubsectionId(subsectionId), command.Name, command.Sequence, command.Description))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveSubsectionAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.RemoveSubsection(new SectionId(sectionId), new SubsectionId(subsectionId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<BillOfQuantitiesDto?> AddSubsectionLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var added = boq.AddSubsectionLineItem(
            new SectionId(sectionId),
            new SubsectionId(subsectionId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            ToVatRate(command.VatRatePercentage),
            command.Sequence,
            command.Notes);

        if (added is null)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> ReviseSubsectionLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        Guid lineItemId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var revised = boq.ReviseSubsectionLineItem(
            new SectionId(sectionId),
            new SubsectionId(subsectionId),
            new LineItemId(lineItemId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            ToVatRate(command.VatRatePercentage),
            command.Sequence,
            command.Notes);

        if (!revised)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveSubsectionLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid subsectionId,
        Guid lineItemId,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null
            || !boq.RemoveSubsectionLineItem(new SectionId(sectionId), new SubsectionId(subsectionId), new LineItemId(lineItemId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<BillOfQuantitiesDto?> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        boq.Submit(now);

        // Submitting a quote is its receipt against the bid: move the owning bid to BoqReceived. The
        // BoQ↔bid link is carried canonically by BillOfQuantities.BidId; LinkBoq records receipt.
        var bid = await bids.GetAsync(boq.BidId, cancellationToken);
        bid?.LinkBoq(boq.Id, now);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BoqExportFile?> ExportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        // Resolve the owning bid's contractor and work package for the file name. A bid references
        // exactly one of each, and there is one BoQ per bid, so this is a single contractor + WP.
        var bid = await bids.GetAsync(boq.BidId, cancellationToken);
        var contractor = bid is null ? null : await contractors.GetAsync(bid.ContractorId, cancellationToken);
        var workPackage = bid is null ? null : await workPackages.GetAsync(bid.WorkPackageId, cancellationToken);

        // The "U.M." column shows unit codes, but line items reference units by id — build the lookup
        // once (including retired units, so a deactivated unit on an old line still renders its code).
        var allUnits = await units.ListAsync(includeInactive: true, cancellationToken);
        var unitCodes = allUnits.ToDictionary(u => u.Id.Value, u => u.Code);

        var model = new BoqExportModel(
            ToDto(boq),
            contractor?.Name ?? "Contractor",
            workPackage?.Name ?? "Deviz",
            unitCodes,
            timeProvider.GetUtcNow());

        return exporter.Export(model);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return false;
        }

        repository.Remove(boq);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    // Line items normalise onto the controlled vocabulary: only an active canonical unit may be used.
    private async Task EnsureActiveUnitAsync(Guid unitOfMeasureId, CancellationToken cancellationToken)
    {
        var unit = await units.GetAsync(new UnitOfMeasureId(unitOfMeasureId), cancellationToken);
        if (unit is null || !unit.IsActive)
        {
            throw new DomainConflictException("The line item must reference an active unit of measure.");
        }
    }

    // Normalise a hex SHA-256 digest the same way the aggregate does, so idempotency comparison is stable.
    private static string? NormalizeHash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static Money ToMoney(MoneyDto dto) => new(dto.Amount, dto.Currency);

    // A null/omitted rate falls back to the standard 21% applied to every line by default.
    private static VatRate ToVatRate(decimal? percentage) =>
        percentage is { } value ? new VatRate(value) : VatRate.Standard;

    private static ExchangeRate? ToExchangeRate(ExchangeRateDto? dto) =>
        dto is null ? null : new ExchangeRate(dto.BaseCurrency, dto.QuoteCurrency, dto.Rate, dto.AsOf);

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

    private static BillOfQuantitiesDto ToDto(BillOfQuantities boq) => new(
        boq.Id.Value,
        boq.BidId.Value,
        boq.Reference,
        boq.Status,
        boq.PricingCurrency,
        ToDto(boq.ExchangeRate),
        boq.SubmittedOn,
        boq.ValidUntil,
        ToDto(boq.Total),
        ToDto(boq.TotalWithVat),
        boq.Sections
            .OrderBy(s => s.Sequence)
            .Select(ToDto)
            .ToList(),
        boq.SourceContentHash,
        boq.SourceDocument is null
            ? null
            : new SourceDocumentDto(
                boq.SourceDocument.FileName,
                boq.SourceDocument.Url,
                boq.SourceDocument.UploadedOn,
                boq.SourceDocument.UploadedBy.Value),
        boq.CreatedOn);

    private static SectionDto ToDto(Section section) => new(
        section.Id.Value,
        section.Name,
        section.Sequence,
        section.Description,
        ToDto(section.Subtotal),
        ToDto(section.SubtotalWithVat),
        section.LineItems
            .OrderBy(li => li.Sequence)
            .Select(ToDto)
            .ToList(),
        section.Subsections
            .OrderBy(s => s.Sequence)
            .Select(ToDto)
            .ToList());

    private static SubsectionDto ToDto(Subsection subsection) => new(
        subsection.Id.Value,
        subsection.Name,
        subsection.Sequence,
        subsection.Description,
        ToDto(subsection.Subtotal),
        ToDto(subsection.SubtotalWithVat),
        subsection.LineItems
            .OrderBy(li => li.Sequence)
            .Select(ToDto)
            .ToList());

    private static LineItemDto ToDto(LineItem item) => new(
        item.Id.Value,
        item.Description,
        item.Quantity,
        item.UnitOfMeasureId.Value,
        ToDto(item.UnitPrice),
        item.VatRate.Percentage,
        ToDto(item.UnitPriceWithVat),
        ToDto(item.LineTotal),
        ToDto(item.LineTotalWithVat),
        item.Sequence,
        item.Notes);

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);

    private static ExchangeRateDto? ToDto(ExchangeRate? rate) =>
        rate is null ? null : new ExchangeRateDto(rate.BaseCurrency, rate.QuoteCurrency, rate.Rate, rate.AsOf);
}
