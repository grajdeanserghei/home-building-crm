using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.ValuationCatalogs;

/// <summary>
/// Thin orchestration over the <see cref="ValuationCatalog"/> aggregate: load via the repository port,
/// invoke domain behaviour, commit through the unit of work. It owns the two cross-aggregate guards the
/// domain deliberately defers: <b>one catalog per project</b> (checked here, backed by a unique index),
/// and <b>validating a BoQ mapping</b> — that the target section/subsection actually exists on the BoQ and
/// that a subsection link carries the subsection's <i>real</i> parent section, so the aggregate can enforce
/// no-double-count and granularity-exclusivity from the link tuples alone. Audit fields are stamped inside
/// the unit of work.
/// </summary>
public sealed class ValuationCatalogAppService(
    IValuationCatalogRepository repository,
    IProjectRepository projects,
    IBillOfQuantitiesRepository billsOfQuantities,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IValuationCatalogAppService
{
    public async Task<ValuationCatalogDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetByProjectAsync(new ProjectId(projectId), cancellationToken);
        return catalog is null ? null : ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        return catalog is null ? null : ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> CreateAsync(
        Guid projectId,
        CreateValuationCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        // The owning project must exist before a catalog references it by id.
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        // One catalog per project. Backed by a unique index on projectId; checked here for a clean 409.
        var existing = await repository.GetByProjectAsync(project.Id, cancellationToken);
        if (existing is not null)
        {
            throw new DomainConflictException(
                "This project already has a valuation catalog. Edit it in place instead of creating another.",
                code: "ValuationCatalogAlreadyExistsForProject",
                parameters: new Dictionary<string, object?> { ["projectId"] = projectId });
        }

        var catalog = ValuationCatalog.Create(
            project.Id,
            command.CatalogReference,
            command.Currency,
            new VatRate(command.VatRatePercentage),
            command.BuiltArea,
            command.GrossFloorArea,
            command.UsableArea,
            command.OwnRegieAdjustment,
            timeProvider.GetUtcNow(),
            command.Method);

        repository.Add(catalog);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> UpdateHeaderAsync(
        Guid id,
        UpdateValuationCatalogHeaderCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        catalog.UpdateHeader(
            command.CatalogReference,
            command.BuiltArea,
            command.GrossFloorArea,
            command.UsableArea,
            command.OwnRegieAdjustment);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        catalog.Activate(timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> ChangeVatRateAsync(
        Guid id,
        ChangeVatRateCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        catalog.ChangeVatRate(new VatRate(command.Percentage), timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> AddItemAsync(
        Guid id,
        AddValuationItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        catalog.AddItem(
            command.Sequence,
            command.PrintedNumber,
            command.Name,
            command.Unit,
            command.CatalogSource,
            command.CostWeight,
            ToMoney(command.UnitCostPerBuiltArea),
            ToMoney(command.TotalCostWithoutVat),
            timeProvider.GetUtcNow());

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> ReviseItemAsync(
        Guid id,
        Guid itemId,
        ReviseValuationItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        var revised = catalog.ReviseItem(
            new ValuationCatalogItemId(itemId),
            command.Sequence,
            command.PrintedNumber,
            command.Name,
            command.Unit,
            command.CatalogSource,
            command.CostWeight,
            ToMoney(command.UnitCostPerBuiltArea),
            ToMoney(command.TotalCostWithoutVat));

        if (!revised)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> DeactivateItemAsync(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null || !catalog.DeactivateItem(new ValuationCatalogItemId(itemId)))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> LinkBoqSectionAsync(
        Guid id,
        Guid itemId,
        LinkBoqSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        // Resolve + validate the target against the real BoQ, populating a subsection's actual parent
        // section so the aggregate can enforce granularity-exclusivity from the tuple alone.
        var link = await ResolveLinkAsync(command, cancellationToken);

        if (!catalog.LinkBoqSection(new ValuationCatalogItemId(itemId), link))
        {
            // The item is not in this catalog.
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<ValuationCatalogDto?> UnlinkBoqSectionAsync(
        Guid id,
        Guid itemId,
        LinkBoqSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        // Unlink matches the stored value object by equality. The client round-trips the link's real
        // parent SectionId (it is in the DTO), so unlinking needs no BoQ lookup — which also lets a
        // mapping to a since-deleted BoQ still be removed.
        if (command.SectionId is not { } sectionId)
        {
            throw new DomainValidationException(
                "A section id is required to unlink a BoQ mapping.",
                nameof(command.SectionId),
                code: "ValuationLinkSectionRequired");
        }

        var link = new ValuationItemLink(
            new BoqId(command.BoqId),
            new SectionId(sectionId),
            command.SubsectionId is { } sub ? new SubsectionId(sub) : null);

        if (!catalog.UnlinkBoqSection(new ValuationCatalogItemId(itemId), link))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(catalog);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var catalog = await repository.GetAsync(new ValuationCatalogId(id), cancellationToken);
        if (catalog is null)
        {
            return false;
        }

        repository.Remove(catalog);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    // Build a valid ValuationItemLink against the real BoQ: a subsection link is populated with the
    // subsection's actual parent section (validating any client-sent section id against it); a
    // whole-section link is validated to point at a real section. Missing BoQ/section/subsection throws.
    private async Task<ValuationItemLink> ResolveLinkAsync(LinkBoqSectionCommand command, CancellationToken cancellationToken)
    {
        var boqId = new BoqId(command.BoqId);
        var boq = await billsOfQuantities.GetAsync(boqId, cancellationToken)
            ?? throw new DomainValidationException(
                "The mapped bill of quantities does not exist.",
                nameof(command.BoqId),
                code: "ValuationLinkBoqNotFound");

        if (command.SubsectionId is { } subGuid)
        {
            var subsectionId = new SubsectionId(subGuid);
            var parent = boq.Sections.FirstOrDefault(s => s.Subsections.Any(ss => ss.Id == subsectionId))
                ?? throw new DomainValidationException(
                    "The mapped BoQ subsection does not exist.",
                    nameof(command.SubsectionId),
                    code: "ValuationLinkSubsectionNotFound");

            // A client-supplied section id must agree with the subsection's real parent.
            if (command.SectionId is { } claimed && new SectionId(claimed) != parent.Id)
            {
                throw new DomainValidationException(
                    "The supplied section id is not the parent of the mapped subsection.",
                    nameof(command.SectionId),
                    code: "ValuationLinkSectionMismatch");
            }

            return new ValuationItemLink(boqId, parent.Id, subsectionId);
        }

        if (command.SectionId is not { } sectionGuid)
        {
            throw new DomainValidationException(
                "A section id (or a subsection id) is required to map a BoQ section.",
                nameof(command.SectionId),
                code: "ValuationLinkSectionRequired");
        }

        var sectionId = new SectionId(sectionGuid);
        if (boq.Sections.All(s => s.Id != sectionId))
        {
            throw new DomainValidationException(
                "The mapped BoQ section does not exist.",
                nameof(command.SectionId),
                code: "ValuationLinkSectionNotFound");
        }

        return new ValuationItemLink(boqId, sectionId);
    }

    private static Money ToMoney(MoneyDto dto) => new(dto.Amount, dto.Currency);

    private static ValuationCatalogDto ToDto(ValuationCatalog catalog) => new(
        catalog.Id.Value,
        catalog.ProjectId.Value,
        catalog.Method,
        catalog.CatalogReference,
        catalog.Status,
        catalog.Currency,
        catalog.VatRate.Percentage,
        catalog.BuiltArea,
        catalog.GrossFloorArea,
        catalog.UsableArea,
        catalog.OwnRegieAdjustment,
        catalog.Items
            .OrderBy(i => i.Sequence)
            .Select(ToDto)
            .ToList(),
        catalog.CreatedOn);

    private static ValuationCatalogItemDto ToDto(ValuationCatalogItem item) => new(
        item.Id.Value,
        item.Sequence,
        item.PrintedNumber,
        item.Name,
        item.Unit,
        item.CatalogSource,
        item.CostWeight,
        ToDto(item.UnitCostPerBuiltArea),
        ToDto(item.TotalCostWithoutVat),
        ToDto(item.TotalCostWithVat),
        item.IsActive,
        item.Links
            .Select(l => new ValuationItemLinkDto(l.BoqId.Value, l.SectionId.Value, l.SubsectionId?.Value))
            .ToList());

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);
}
