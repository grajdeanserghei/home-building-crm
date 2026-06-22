using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.UnitsOfMeasure.Events;

/// <summary>
/// Raised when a unit is retired (deactivated) or brought back into use (activated). Retirement
/// is preferred over deletion because historical line items may still reference the unit.
/// </summary>
public sealed record UnitOfMeasureActivationChanged(
    UnitOfMeasureId UnitOfMeasureId,
    bool IsActive,
    DateTimeOffset OccurredOn) : IDomainEvent;
