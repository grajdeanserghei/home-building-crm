using HomeProjectManagement.Domain.Bids;

namespace HomeProjectManagement.Application.Bids;

/// <summary>
/// Read model returned to clients. <c>Notes</c> is the discussion log (oldest first);
/// <c>CreatedAt</c> comes from the aggregate's audit fields.
/// </summary>
public sealed record BidDto(
    Guid Id,
    Guid WorkPackageId,
    Guid ContractorId,
    BidStatus Status,
    DateTimeOffset? FirstContactedOn,
    string? Summary,
    IReadOnlyList<DiscussionNoteDto> Notes,
    DateTimeOffset CreatedAt);

/// <summary>A single entry in a bid's discussion log (mirrors the <c>DiscussionNote</c> entity).</summary>
public sealed record DiscussionNoteDto(
    Guid Id,
    NoteType Type,
    DateTimeOffset OccurredOn,
    Guid AuthorId,
    string Content);

/// <summary>
/// Input for opening a contractor's bid on a work package. The owning work package comes from
/// the route, not the body. A freshly opened bid starts in status <c>InDiscussion</c>.
/// </summary>
public sealed record OpenBidCommand(
    Guid ContractorId,
    DateTimeOffset? FirstContactedOn,
    string? Summary);

/// <summary>Input for editing a bid's summary and first-contact date.</summary>
/// <remarks>
/// Status is intentionally not mutated here: bid lifecycle transitions (notably selection, which
/// rejects the competing bids and awards the work package) carry invariants, so they go through
/// the dedicated change-status endpoint, not a free-form edit.
/// </remarks>
public sealed record UpdateBidCommand(
    string? Summary,
    DateTimeOffset? FirstContactedOn);

/// <summary>
/// Input for transitioning a bid's status. <c>Selected</c> additionally rejects the competing
/// bids on the same work package (coordinated by the application service).
/// </summary>
public sealed record ChangeBidStatusCommand(BidStatus Status);

/// <summary>Input for appending a note to a bid's discussion log. The author is the current user.</summary>
public sealed record LogDiscussionNoteCommand(
    NoteType Type,
    DateTimeOffset OccurredOn,
    string Content);
