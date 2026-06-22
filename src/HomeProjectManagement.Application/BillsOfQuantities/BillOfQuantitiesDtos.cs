using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.BillsOfQuantities;

/// <summary>
/// Read model returned to clients. <c>Total</c>, each section's <c>Subtotal</c>, and each line's
/// <c>LineTotal</c> are derived in the aggregate; <c>CreatedAt</c> comes from the audit fields.
/// </summary>
public sealed record BillOfQuantitiesDto(
    Guid Id,
    Guid BidId,
    string? Reference,
    int Version,
    BoqStatus Status,
    Currency PricingCurrency,
    ExchangeRateDto? ExchangeRate,
    DateTimeOffset? SubmittedOn,
    DateTimeOffset? ValidUntil,
    MoneyDto Total,
    IReadOnlyList<SectionDto> Sections,
    DateTimeOffset CreatedAt);

/// <summary>A section of the BoQ with its line items (mirrors the <c>Section</c> entity).</summary>
public sealed record SectionDto(
    Guid Id,
    string Name,
    int Sequence,
    string? Description,
    MoneyDto Subtotal,
    IReadOnlyList<LineItemDto> LineItems);

/// <summary>A priced line within a section (mirrors the <c>LineItem</c> entity).</summary>
public sealed record LineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    MoneyDto UnitPrice,
    MoneyDto LineTotal,
    int Sequence,
    string? Notes);

/// <summary>A monetary amount (mirrors the <c>Money</c> value object).</summary>
public sealed record MoneyDto(decimal Amount, Currency Currency);

/// <summary>A pinned conversion rate (mirrors the <c>ExchangeRate</c> value object).</summary>
public sealed record ExchangeRateDto(
    Currency BaseCurrency,
    Currency QuoteCurrency,
    decimal Rate,
    DateOnly AsOf);

/// <summary>
/// Input for drafting a new BoQ version within a bid. The owning bid comes from the route; the
/// version number is assigned by the service (next after the bid's existing versions). A freshly
/// drafted BoQ starts in status <c>Draft</c> with no sections.
/// </summary>
public sealed record DraftBillOfQuantitiesCommand(
    Currency PricingCurrency,
    string? Reference,
    ExchangeRateDto? ExchangeRate,
    DateTimeOffset? SubmittedOn,
    DateTimeOffset? ValidUntil);

/// <summary>Input for editing a BoQ's header details. The pricing currency is fixed at draft time.</summary>
public sealed record UpdateBillOfQuantitiesCommand(
    string? Reference,
    ExchangeRateDto? ExchangeRate,
    DateTimeOffset? SubmittedOn,
    DateTimeOffset? ValidUntil);

/// <summary>Input for transitioning a BoQ's status (Draft → Submitted → Accepted / Rejected / Withdrawn).</summary>
public sealed record ChangeBoqStatusCommand(BoqStatus Status);

/// <summary>Input for adding or editing a section.</summary>
public sealed record SectionCommand(string Name, int Sequence, string? Description);

/// <summary>
/// Input for adding or editing a line item. The unit price must be in the BoQ's pricing currency,
/// and the unit of measure must reference an active canonical unit.
/// </summary>
public sealed record LineItemCommand(
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    MoneyDto UnitPrice,
    int Sequence,
    string? Notes);
