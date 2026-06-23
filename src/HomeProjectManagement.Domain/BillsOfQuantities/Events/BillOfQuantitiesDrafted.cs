using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities.Events;

/// <summary>
/// Raised when the Bill of Quantities is drafted for a bid — the start of capturing a contractor's
/// priced quote (<c>deviz</c>). There is at most one BoQ per bid.
/// </summary>
public sealed record BillOfQuantitiesDrafted(
    BoqId BoqId,
    BidId BidId,
    DateTimeOffset OccurredOn) : IDomainEvent;
