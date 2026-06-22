using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities.Events;

/// <summary>
/// Raised when a Bill of Quantities transitions status (e.g. Draft → Submitted → Accepted). A
/// move to <see cref="BoqStatus.Accepted"/> is what a contract is created from, coordinated by an
/// application service.
/// </summary>
public sealed record BillOfQuantitiesStatusChanged(
    BoqId BoqId,
    BoqStatus From,
    BoqStatus To,
    DateTimeOffset OccurredOn) : IDomainEvent;
