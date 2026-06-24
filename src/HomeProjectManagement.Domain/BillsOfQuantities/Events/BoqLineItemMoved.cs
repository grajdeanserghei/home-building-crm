using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities.Events;

/// <summary>
/// Raised when a line item is reordered within its container or moved to another container (a
/// section's direct list or a subsection) inside the same Bill of Quantities. The affected
/// containers are renumbered to a dense 1..N <c>Sequence</c>.
/// </summary>
public sealed record BoqLineItemMoved(
    BoqId BoqId,
    BidId BidId,
    LineItemId LineItemId,
    DateTimeOffset OccurredOn) : IDomainEvent;
