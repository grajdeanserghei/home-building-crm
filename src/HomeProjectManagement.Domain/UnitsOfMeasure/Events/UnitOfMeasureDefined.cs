using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.UnitsOfMeasure.Events;

/// <summary>Raised when a new canonical unit of measure is defined in the vocabulary.</summary>
public sealed record UnitOfMeasureDefined(
    UnitOfMeasureId UnitOfMeasureId,
    string Code,
    string Name,
    UnitCategory Category,
    DateTimeOffset OccurredOn) : IDomainEvent;
