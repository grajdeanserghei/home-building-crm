using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities.Events;

/// <summary>
/// Raised when a new Bill of Quantities version is drafted within a bid — the start of capturing
/// a contractor's priced quote (<c>deviz</c>).
/// </summary>
public sealed record BillOfQuantitiesDrafted(
    BoqId BoqId,
    BidId BidId,
    int Version,
    DateTimeOffset OccurredOn) : IDomainEvent;
