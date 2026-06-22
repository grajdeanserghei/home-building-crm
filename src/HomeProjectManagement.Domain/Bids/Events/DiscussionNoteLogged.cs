using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids.Events;

/// <summary>Raised when a note is added to a bid's discussion log.</summary>
public sealed record DiscussionNoteLogged(
    BidId BidId,
    DiscussionNoteId NoteId,
    NoteType Type,
    DateTimeOffset OccurredOn) : IDomainEvent;
