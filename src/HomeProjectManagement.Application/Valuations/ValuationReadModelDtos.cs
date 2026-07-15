using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.ConstructionValuations;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.Valuations;

// ---------------------------------------------------------------------------------------------------
// Estimate vs. real BoQ cost (live, catalog-scoped)
// ---------------------------------------------------------------------------------------------------

/// <summary>
/// Read model comparing each appraiser catalog item's (net) estimate against the owners' real BoQ cost
/// rolled up from that item's mappings. All money is in the catalog currency (RON); BoQ subtotals priced
/// in another currency are converted with the app-wide rate and per-apartment BoQs are scaled to the whole
/// build (the same basis as the project budget). <c>Net-to-net</c> — col G is VAT-exclusive.
/// </summary>
public sealed record ValuationVsBoqDto(
    Guid ProjectId,
    Guid ValuationCatalogId,
    Currency Currency,
    decimal RonPerEur,
    IReadOnlyList<ValuationVsBoqItemDto> Items,
    IReadOnlyList<ValuationCoverageGapDto> CoverageGaps,
    ValuationVsBoqTotalsDto Totals);

/// <summary>
/// One catalog item's comparison line. <see cref="Actual"/>, <see cref="Variance"/> and
/// <see cref="VariancePercentage"/> are null when the item has no BoQ mapping (a <c>%</c> catch-all): it is
/// reported as a coverage gap, not as a −100% variance.
/// </summary>
public sealed record ValuationVsBoqItemDto(
    Guid ValuationCatalogItemId,
    int Sequence,
    string Name,
    bool IsMapped,
    MoneyDto Estimate,
    MoneyDto? Actual,
    MoneyDto? Variance,
    decimal? VariancePercentage,
    IReadOnlyList<ValuationVsBoqLinkDto> Links);

/// <summary>One mapping's contribution to an item's actual cost (converted + scaled to the whole build).</summary>
public sealed record ValuationVsBoqLinkDto(
    Guid BoqId,
    Guid SectionId,
    Guid? SubsectionId,
    bool BoqResolved,
    MoneyDto Contribution);

/// <summary>
/// A gap in coverage. <c>UnmappedItem</c>: an estimated item mapped to no BoQ section. <c>UnattributedBoqLines</c>:
/// real BoQ cost under a subsection-mapped section that no mapping covers (the section's direct lines and any
/// unmapped subsections) — surfaced so it does not silently vanish.
/// </summary>
public sealed record ValuationCoverageGapDto(
    string Kind,
    Guid? ValuationCatalogItemId,
    Guid? BoqId,
    Guid? SectionId,
    string Description,
    MoneyDto Amount);

/// <summary>
/// Project-level rollup. <see cref="CoveragePercentage"/> is the share of estimated value that is mapped
/// ("X% of estimated value is mapped"). <see cref="TotalUnattributedBoqCost"/> is real BoQ cost under
/// subsection-mapped sections that no mapping covers. All money in the catalog currency.
/// </summary>
public sealed record ValuationVsBoqTotalsDto(
    MoneyDto TotalEstimate,
    MoneyDto MappedEstimate,
    MoneyDto TotalActual,
    MoneyDto TotalVariance,
    decimal? TotalVariancePercentage,
    decimal CoveragePercentage,
    MoneyDto TotalUnattributedBoqCost);

// ---------------------------------------------------------------------------------------------------
// Completion progress across snapshots (frozen, catalog-scoped)
// ---------------------------------------------------------------------------------------------------

/// <summary>
/// Read model charting completion over time for a catalog: each snapshot's frozen totals, plus each
/// catalog item's completion series across snapshots. All values read straight from the frozen snapshots.
/// </summary>
public sealed record ValuationProgressSeriesDto(
    Guid ValuationCatalogId,
    IReadOnlyList<ValuationProgressSnapshotDto> Snapshots,
    IReadOnlyList<ValuationItemProgressDto> Items);

/// <summary>One snapshot's headline: when it was assessed and its frozen progress totals.</summary>
public sealed record ValuationProgressSnapshotDto(
    Guid Id,
    DateOnly AssessedOn,
    string? Appraiser,
    decimal RonPerEur,
    ValuationProgressTotalsDto Totals);

/// <summary>One catalog item's completion series across the snapshots that assessed it.</summary>
public sealed record ValuationItemProgressDto(
    Guid ValuationCatalogItemId,
    string Name,
    IReadOnlyList<ValuationItemProgressPointDto> Points);

/// <summary>One assessed point in an item's series (all money frozen from the snapshot).</summary>
public sealed record ValuationItemProgressPointDto(
    Guid ConstructionValuationId,
    DateOnly AssessedOn,
    decimal CompletionPercentage,
    MoneyDto EstimatedValueWithVat,
    MoneyDto CompletedValueWithVat,
    MoneyDto RemainingValueWithVat);
