using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contractors;
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
        var copy = Bid.DuplicateFrom(source, timeProvider.GetUtcNow());
        copy.Relabel(source.Label is null ? null : $"{source.Label} (copie)");

        repository.Add(copy);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(copy);
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
