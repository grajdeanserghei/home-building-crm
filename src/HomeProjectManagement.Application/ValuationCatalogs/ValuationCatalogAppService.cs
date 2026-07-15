using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs;
using HomeProjectManagement.Domain.WorkPackages;

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
    IBidRepository bids,
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

        // WorkPackageId is not part of link identity, so a placeholder matches the stored link by
        // equality — unlinking still needs no BoQ/bid lookup (and works for a since-deleted BoQ). The
        // client round-trips the link's real section/subsection/line ids (they are in the DTO).
        var link = new ValuationItemLink(
            new BoqId(command.BoqId),
            default,
            new SectionId(sectionId),
            command.SubsectionId is { } sub ? new SubsectionId(sub) : null,
            command.LineItemId is { } line ? new LineItemId(line) : null);

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

        // The BoQ competes for exactly one work package (boq → bid → workPackage). Stamped on the link so
        // the read model can treat competing BoQs of one work package as alternatives, not additive parts.
        var workPackageId = await ResolveWorkPackageAsync(boq, cancellationToken);

        // Line-level link (finest granularity): resolve the line's actual parent section/subsection so the
        // aggregate can police granularity from the tuple alone. Takes precedence over section/subsection.
        if (command.LineItemId is { } lineGuid)
        {
            var lineItemId = new LineItemId(lineGuid);
            var line = boq.LineItems.FirstOrDefault(li => li.Id == lineItemId)
                ?? throw new DomainValidationException(
                    "The mapped BoQ line item does not exist.",
                    nameof(command.LineItemId),
                    code: "ValuationLinkLineItemNotFound");

            // Any client-supplied section/subsection ids must agree with the line's real parents.
            if (command.SectionId is { } claimedSection && new SectionId(claimedSection) != line.SectionId)
            {
                throw new DomainValidationException(
                    "The supplied section id is not the parent of the mapped line item.",
                    nameof(command.SectionId),
                    code: "ValuationLinkSectionMismatch");
            }

            if (command.SubsectionId is { } claimedSub
                && (line.SubsectionId is null || new SubsectionId(claimedSub) != line.SubsectionId))
            {
                throw new DomainValidationException(
                    "The supplied subsection id is not the parent of the mapped line item.",
                    nameof(command.SubsectionId),
                    code: "ValuationLinkSubsectionMismatch");
            }

            return new ValuationItemLink(boqId, workPackageId, line.SectionId, line.SubsectionId, lineItemId);
        }

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

            return new ValuationItemLink(boqId, workPackageId, parent.Id, subsectionId);
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

        return new ValuationItemLink(boqId, workPackageId, sectionId);
    }

    // Resolve the work package a BoQ competes for: boq → bid → workPackage. The bid should always exist
    // (a BoQ references its bid by identity); a dangling reference is a data error, surfaced as validation.
    private async Task<WorkPackageId> ResolveWorkPackageAsync(BillOfQuantities boq, CancellationToken cancellationToken)
    {
        var bid = await bids.GetAsync(boq.BidId, cancellationToken)
            ?? throw new DomainValidationException(
                "The mapped BoQ's bid does not exist.",
                nameof(boq.BidId),
                code: "ValuationLinkBidNotFound");

        return bid.WorkPackageId;
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
            .Select(l => new ValuationItemLinkDto(
                l.BoqId.Value, l.SectionId.Value, l.SubsectionId?.Value, l.LineItemId?.Value))
            .ToList());

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);
}
