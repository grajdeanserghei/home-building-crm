using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities.Events;

/// <summary>
/// Raised when a Bill of Quantities has its contents replaced in place — a revised <c>deviz</c>
/// supersedes the previous one on the same BoQ (its sections were dropped and the provenance
/// re-pointed) rather than a new version being created.
/// </summary>
public sealed record BillOfQuantitiesContentsReplaced(
    BoqId BoqId,
    BidId BidId,
    DateTimeOffset OccurredOn) : IDomainEvent;
