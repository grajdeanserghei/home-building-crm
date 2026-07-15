using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs.Events;

/// <summary>
/// Raised when a valuation catalog's VAT rate changes; every item's stored <c>TotalCostWithVat</c> is
/// recomputed as part of the same operation. Past snapshots are unaffected.
/// </summary>
public sealed record ValuationCatalogVatRateChanged(
    ValuationCatalogId ValuationCatalogId,
    decimal FromPercentage,
    decimal ToPercentage,
    DateTimeOffset OccurredOn) : IDomainEvent;
