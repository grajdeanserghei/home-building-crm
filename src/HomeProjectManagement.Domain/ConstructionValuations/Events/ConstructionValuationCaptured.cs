using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Domain.ConstructionValuations.Events;

/// <summary>Raised when a dated construction-valuation snapshot is captured against a valuation catalog.</summary>
public sealed record ConstructionValuationCaptured(
    ConstructionValuationId ConstructionValuationId,
    ValuationCatalogId ValuationCatalogId,
    DateOnly AssessedOn,
    DateTimeOffset OccurredOn) : IDomainEvent;
