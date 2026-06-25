namespace HomeProjectManagement.Application.Bids;

/// <summary>
/// Driving (primary) port for bid use cases — the selection process for a work-package/contractor
/// pair. The minimal-API endpoints in ApiService are the adapter that calls this; the host never
/// touches EF Core or the domain directly.
/// </summary>
public interface IBidAppService
{
    /// <summary>The bids competing for one work package.</summary>
    Task<IReadOnlyList<BidDto>> ListByWorkPackageAsync(Guid workPackageId, CancellationToken cancellationToken = default);

    /// <summary>Every bid a contractor has participated in.</summary>
    Task<IReadOnlyList<BidDto>> ListByContractorAsync(Guid contractorId, CancellationToken cancellationToken = default);

    Task<BidDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Open a contractor's bid on a work package. Returns null if the work package or the
    /// contractor does not exist. A contractor may hold several bids on one package (variants).
    /// </summary>
    Task<BidDto?> OpenAsync(Guid workPackageId, OpenBidCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicate an existing bid in place — same work package and contractor, carrying over its
    /// summary, first-contact date and (suffixed) label, with a fresh discussion log. Returns null
    /// if the source bid does not exist.
    /// </summary>
    Task<BidDto?> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BidDto?> UpdateAsync(Guid id, UpdateBidCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transition a bid's status. Returns null if the bid does not exist; throws
    /// <see cref="InvalidOperationException"/> for an illegal transition. Setting the status to
    /// <c>Selected</c> is rejected here — a bid is selected only by awarding its contract (the atomic
    /// award flow on the Contract app service), which also rejects the rivals and awards the work package.
    /// </summary>
    Task<BidDto?> ChangeStatusAsync(Guid id, ChangeBidStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>Append a note to the bid's discussion log. Returns null if the bid does not exist.</summary>
    Task<BidDto?> LogNoteAsync(Guid id, LogDiscussionNoteCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a note from the bid's discussion log. Returns false if the bid or note is absent.</summary>
    Task<bool> RemoveNoteAsync(Guid id, Guid noteId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
