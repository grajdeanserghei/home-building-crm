using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids.Events;

/// <summary>
/// Raised when a priced BoQ is received against the bid and it moves to
/// <see cref="BidStatus.BoqReceived"/>. <see cref="BoqId"/> identifies the received bill; the
/// canonical link is carried by <c>BillOfQuantities.BidId</c>.
/// </summary>
public sealed record BidBoqReceived(
    BidId BidId, BoqId BoqId, DateTimeOffset OccurredOn) : IDomainEvent;
