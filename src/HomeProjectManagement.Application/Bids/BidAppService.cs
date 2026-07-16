using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.ValuationCatalogs;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Bids;

/// <summary>
/// Thin orchestration over the <see cref="Bid"/> aggregate: load via the repository port, invoke
/// domain behaviour, commit through the unit of work. A contractor may hold several bids on one
/// work package (variants), so opening is unconstrained. Selecting a bid is <b>not</b> done here —
/// it is one inseparable part of the atomic award flow in the Contract app service, so requests to
/// set a bid to <c>Selected</c> are rejected with a pointer there. Audit fields are stamped inside
/// the unit of work.
/// </summary>
public sealed class BidAppService(
    IBidRepository repository,
    IWorkPackageRepository workPackages,
    IContractorRepository contractors,
    IBillOfQuantitiesRepository billsOfQuantities,
    IValuationCatalogRepository valuationCatalogs,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBidAppService
{
    public async Task<IReadOnlyList<BidDto>> ListByWorkPackageAsync(
        Guid workPackageId,
        CancellationToken cancellationToken = default)
    {
        var bids = await repository.ListByWorkPackageAsync(new WorkPackageId(workPackageId), cancellationToken);
        return bids.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BidDto>> ListByContractorAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        var bids = await repository.ListByContractorAsync(new ContractorId(contractorId), cancellationToken);
        return bids.Select(ToDto).ToList();
    }

    public async Task<BidDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        return bid is null ? null : ToDto(bid);
    }

    public async Task<BidDto?> OpenAsync(
        Guid workPackageId,
        OpenBidCommand command,
        CancellationToken cancellationToken = default)
    {
        // Both parents must exist before opening a bid that references them by id.
        var workPackage = await workPackages.GetAsync(new WorkPackageId(workPackageId), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        var contractor = await contractors.GetAsync(new ContractorId(command.ContractorId), cancellationToken);
        if (contractor is null)
        {
            return null;
        }

        var bid = Bid.Open(
            workPackage.Id,
            contractor.Id,
            timeProvider.GetUtcNow(),
            command.FirstContactedOn,
            command.Summary,
            command.Label);

        repository.Add(bid);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(bid);
    }

    public async Task<BidDto?> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await repository.GetAsync(new BidId(id), cancellationToken);
        if (source is null)
        {
            return null;
        }

        // Clone in place: same work package and contractor, a fresh discussion log. Mark the copy
        // so the two variants are distinguishable at a glance.
        var now = timeProvider.GetUtcNow();
        var copy = Bid.DuplicateFrom(source, now);
        copy.Relabel(source.Label is null ? null : $"{source.Label} (copie)");

        repository.Add(copy);

        // Carry the offer's priced BoQ over too (sections, subsections, line items), if it has one, so
        // the duplicate is a usable variant rather than an empty shell. Persisted in the same commit.
        var sourceBoq = await billsOfQuantities.GetByBidAsync(source.Id, cancellationToken);
        if (sourceBoq is not null)
        {
            var boqCopy = BillOfQuantities.CopyFor(sourceBoq, copy.Id, now);
            billsOfQuantities.Add(boqCopy.Bill);

            // The valuation catalog maps its items to this BoQ's sections/lines by id; carry those
            // mappings over to the copy (translated to the new ids) so the duplicate is comparable too.
            await CopyValuationLinksAsync(source, sourceBoq, boqCopy, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(copy);
    }

    // Re-create, against the copied BoQ, every valuation-catalog link that pointed at the source BoQ.
    // The catalog is loaded through its repository (same DbContext), so mutating it here is persisted by
    // the caller's commit. Best-effort: a project with no catalog, or a BoQ with no links, is a no-op.
    private async Task CopyValuationLinksAsync(
        Bid source,
        BillOfQuantities sourceBoq,
        BoqCopy boqCopy,
        CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.GetAsync(source.WorkPackageId, cancellationToken);
        if (workPackage is null)
        {
            return;
        }

        var catalog = await valuationCatalogs.GetByProjectAsync(workPackage.ProjectId, cancellationToken);
        if (catalog is null)
        {
            return;
        }

        // Snapshot the links that target the source BoQ before mutating: LinkBoqSection adds to the same
        // owned collections we are iterating, which would otherwise throw mid-enumeration.
        var linksToCopy = catalog.Items
            .SelectMany(item => item.Links
                .Where(link => link.BoqId == sourceBoq.Id)
                .Select(link => (item.Id, link)))
            .ToList();

        foreach (var (itemId, link) in linksToCopy)
        {
            var translated = new ValuationItemLink(
                boqCopy.Bill.Id,
                link.WorkPackageId,
                boqCopy.SectionMap[link.SectionId],
                link.SubsectionId is { } subsectionId ? boqCopy.SubsectionMap[subsectionId] : null,
                link.LineItemId is { } lineItemId ? boqCopy.LineItemMap[lineItemId] : null);

            catalog.LinkBoqSection(itemId, translated);
        }
    }

    public async Task<BidDto?> UpdateAsync(
        Guid id,
        UpdateBidCommand command,
        CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        if (bid is null)
        {
            return null;
        }

        bid.Summarize(command.Summary);
        bid.SetFirstContact(command.FirstContactedOn);
        bid.Relabel(command.Label);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(bid);
    }

    public async Task<BidDto?> ChangeStatusAsync(
        Guid id,
        ChangeBidStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        if (bid is null)
        {
            return null;
        }

        if (command.Status == BidStatus.Selected)
        {
            // Selecting a bid is no longer a standalone step: it is one inseparable part of awarding
            // the contract (which also accepts the BoQ, rejects the rivals, and transitions the work
            // package). Route it through the atomic award use case instead.
            throw new DomainConflictException(
                "A bid is selected by awarding its contract; POST /api/contracts with the winning BoQ.");
        }

        var now = timeProvider.GetUtcNow();
        if (command.Status == BidStatus.BoqExpected)
        {
            // BoqExpected carries the date the contractor committed to (already an absolute date).
            bid.ExpectBoqBy(command.ExpectedBoqDate, now);
        }
        else
        {
            bid.ChangeStatus(command.Status, now);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(bid);
    }

    public async Task<BidDto?> LogNoteAsync(
        Guid id,
        LogDiscussionNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        if (bid is null)
        {
            return null;
        }

        bid.LogNote(command.Type, command.OccurredOn, currentUser.UserId, command.Content, timeProvider.GetUtcNow());

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(bid);
    }

    public async Task<bool> RemoveNoteAsync(Guid id, Guid noteId, CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        if (bid is null || !bid.RemoveNote(new DiscussionNoteId(noteId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bid = await repository.GetAsync(new BidId(id), cancellationToken);
        if (bid is null)
        {
            return false;
        }

        repository.Remove(bid);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static BidDto ToDto(Bid bid) => new(
        bid.Id.Value,
        bid.WorkPackageId.Value,
        bid.ContractorId.Value,
        bid.Status,
        bid.FirstContactedOn,
        bid.ExpectedBoqDate,
        bid.Summary,
        bid.Label,
        bid.Notes
            .OrderBy(n => n.OccurredOn)
            .Select(n => new DiscussionNoteDto(n.Id.Value, n.Type, n.OccurredOn, n.AuthorId.Value, n.Content))
            .ToList(),
        bid.CreatedOn);
}
