using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs.Events;

/// <summary>Raised when a priced item is added to a valuation catalog.</summary>
public sealed record ValuationCatalogItemAdded(
    ValuationCatalogId ValuationCatalogId,
    ValuationCatalogItemId ItemId,
    DateTimeOffset OccurredOn) : IDomainEvent;
