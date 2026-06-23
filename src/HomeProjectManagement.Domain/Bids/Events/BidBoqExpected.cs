using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids.Events;

/// <summary>
/// Raised when a contractor commits to send a priced BoQ by a given date and the bid moves to
/// <see cref="BidStatus.BoqExpected"/>. <see cref="ExpectedBoqDate"/> is the absolute date the
/// caller resolved any relative promise to.
/// </summary>
public sealed record BidBoqExpected(
    BidId BidId, DateTimeOffset? ExpectedBoqDate, DateTimeOffset OccurredOn) : IDomainEvent;
