using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.ValuationCatalogs;

/// <summary>
/// Read model for the appraiser's per-project valuation baseline (the <c>fișă de calcul</c>). Strongly
/// typed ids are flattened to <see cref="Guid"/>; domain enums are exposed directly; money is reported
/// via <see cref="MoneyDto"/> (reused from the BoQ read models). <c>TotalCostWithVat</c> is the stored
/// gross the catalog keeps in sync with its VAT rate. Items are ordered by <c>Sequence</c>.
/// </summary>
public sealed record ValuationCatalogDto(
    Guid Id,
    Guid ProjectId,
    ValuationMethod Method,
    string CatalogReference,
    ValuationCatalogStatus Status,
    Currency Currency,
    decimal VatRatePercentage,
    decimal BuiltArea,
    decimal GrossFloorArea,
    decimal UsableArea,
    decimal OwnRegieAdjustment,
    IReadOnlyList<ValuationCatalogItemDto> Items,
    DateTimeOffset CreatedAt);

/// <summary>One priced row of the estimate (mirrors the <c>ValuationCatalogItem</c> entity).</summary>
public sealed record ValuationCatalogItemDto(
    Guid Id,
    int Sequence,
    string PrintedNumber,
    string Name,
    string Unit,
    string CatalogSource,
    decimal CostWeight,
    MoneyDto UnitCostPerBuiltArea,
    MoneyDto TotalCostWithoutVat,
    MoneyDto TotalCostWithVat,
    bool IsActive,
    IReadOnlyList<ValuationItemLinkDto> Links);

/// <summary>
/// A mapping from a catalog item to a real BoQ section, subsection, or single line item — mirrors
/// <c>ValuationItemLink</c>. <see cref="LineItemId"/> set is a line-level link (the finest granularity);
/// its <see cref="SectionId"/>/<see cref="SubsectionId"/> are that line's actual parents.
/// </summary>
public sealed record ValuationItemLinkDto(Guid BoqId, Guid SectionId, Guid? SubsectionId, Guid? LineItemId);

/// <summary>
/// Input for creating a project's valuation catalog. The owning project comes from the route. Starts in
/// status <c>Draft</c>; the method defaults to segregated cost. At most one catalog per project.
/// </summary>
public sealed record CreateValuationCatalogCommand(
    string CatalogReference,
    Currency Currency,
    decimal VatRatePercentage,
    decimal BuiltArea,
    decimal GrossFloorArea,
    decimal UsableArea,
    decimal OwnRegieAdjustment,
    ValuationMethod Method = ValuationMethod.SegregatedCost);

/// <summary>Input for editing a catalog's header (reference + surfaces + own-regie adjustment).</summary>
public sealed record UpdateValuationCatalogHeaderCommand(
    string CatalogReference,
    decimal BuiltArea,
    decimal GrossFloorArea,
    decimal UsableArea,
    decimal OwnRegieAdjustment);

/// <summary>Input for changing the report VAT rate; the catalog recomputes each item's stored gross total.</summary>
public sealed record ChangeVatRateCommand(decimal Percentage);

/// <summary>
/// Input for adding a priced item. Money must be in the catalog currency (the aggregate enforces this).
/// The gross total is computed from the catalog's current VAT rate.
/// </summary>
public sealed record AddValuationItemCommand(
    int Sequence,
    string PrintedNumber,
    string Name,
    string Unit,
    string CatalogSource,
    decimal CostWeight,
    MoneyDto UnitCostPerBuiltArea,
    MoneyDto TotalCostWithoutVat);

/// <summary>Input for revising an existing item's priced fields (same shape as adding one).</summary>
public sealed record ReviseValuationItemCommand(
    int Sequence,
    string PrintedNumber,
    string Name,
    string Unit,
    string CatalogSource,
    decimal CostWeight,
    MoneyDto UnitCostPerBuiltArea,
    MoneyDto TotalCostWithoutVat);

/// <summary>
/// Input for mapping a catalog item to a real BoQ target at one of three granularities. When
/// <see cref="LineItemId"/> is set the mapping is line-level; else when <see cref="SubsectionId"/> is set
/// it is subsection-level; else the whole section is mapped. For the finer levels the application service
/// resolves the target's <b>actual</b> parent section/subsection from the BoQ (any client-sent
/// <see cref="SectionId"/>/<see cref="SubsectionId"/> is validated against it).
/// </summary>
public sealed record LinkBoqSectionCommand(Guid BoqId, Guid? SectionId, Guid? SubsectionId, Guid? LineItemId = null);
