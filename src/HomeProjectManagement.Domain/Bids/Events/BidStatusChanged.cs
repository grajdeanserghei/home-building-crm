using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids.Events;

/// <summary>Raised when a bid transitions from one status to another.</summary>
public sealed record BidStatusChanged(
    BidId BidId,
    BidStatus From,
    BidStatus To,
    DateTimeOffset OccurredOn) : IDomainEvent;
