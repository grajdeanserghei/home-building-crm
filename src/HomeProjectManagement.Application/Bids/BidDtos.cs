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
    DateTimeOffset? ExpectedBoqDate,
    string? Summary,
    string? Label,
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
/// the route, not the body. A freshly opened bid starts in status <c>InDiscussion</c>. The
/// optional <see cref="Label"/> tells apart several bids from the same contractor on one package.
/// </summary>
public sealed record OpenBidCommand(
    Guid ContractorId,
    DateTimeOffset? FirstContactedOn,
    string? Summary,
    string? Label);

/// <summary>Input for editing a bid's summary and first-contact date.</summary>
/// <remarks>
/// Status is intentionally not mutated here: bid lifecycle transitions (notably selection, which
/// rejects the competing bids and awards the work package) carry invariants, so they go through
/// the dedicated change-status endpoint, not a free-form edit.
/// </remarks>
public sealed record UpdateBidCommand(
    string? Summary,
    DateTimeOffset? FirstContactedOn,
    string? Label);

/// <summary>
/// Input for transitioning a bid's status. Moving to <c>BoqExpected</c> records the optional
/// <see cref="ExpectedBoqDate"/> the contractor committed to (an absolute date the caller resolved
/// any relative promise to). <c>Selected</c> is reached only through the award flow, not here.
/// </summary>
public sealed record ChangeBidStatusCommand(BidStatus Status, DateTimeOffset? ExpectedBoqDate = null);

/// <summary>Input for appending a note to a bid's discussion log. The author is the current user.</summary>
public sealed record LogDiscussionNoteCommand(
    NoteType Type,
    DateTimeOffset OccurredOn,
    string Content);
