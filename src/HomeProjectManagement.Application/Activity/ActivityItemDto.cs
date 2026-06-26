using HomeProjectManagement.Domain.Bids;

namespace HomeProjectManagement.Application.Activity;

/// <summary>
/// One entry in a project's recent-activity feed — a flat, read-only shape assembled across
/// aggregates (the project, its work packages, their bids and discussion notes). Per the codebase
/// convention the server carries structured fields and the client composes the human sentence via
/// i18n, so this DTO holds the parts (kind, who, when, which work package/bid, the note text) rather
/// than a pre-rendered string.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> discriminates the entry. Fields are populated as relevant:
/// <list type="bullet">
/// <item><c>NoteLogged</c> — a discussion note: <see cref="NoteType"/>, <see cref="Content"/>,
/// <see cref="AuthorId"/>, plus the bid/contractor/work-package context. <see cref="Timestamp"/> is
/// the note's <i>OccurredOn</i> (when the interaction happened).</item>
/// <item><c>BidOpened</c> — a bid was opened: contractor + work-package context; timestamp is the
/// bid's creation time.</item>
/// <item><c>WorkPackageAdded</c> — a work package was defined; timestamp is its creation time.</item>
/// <item><c>ProjectCreated</c> — the project itself was created.</item>
/// </list>
/// Attribution (<see cref="AuthorId"/>) is the stakeholder's user id; name resolution does not exist
/// server-side yet (single stub user), so the client maps the id to a display name with a fallback.
/// </remarks>
public sealed record ActivityItemDto(
    string Kind,
    DateTimeOffset Timestamp,
    Guid? AuthorId,
    Guid? WorkPackageId,
    string? WorkPackageName,
    Guid? BidId,
    string? ContractorName,
    NoteType? NoteType,
    string? Content);
