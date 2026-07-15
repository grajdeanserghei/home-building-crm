using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs.Events;

/// <summary>Raised when a draft valuation catalog becomes the project's active baseline.</summary>
public sealed record ValuationCatalogActivated(
    ValuationCatalogId ValuationCatalogId,
    DateTimeOffset OccurredOn) : IDomainEvent;
