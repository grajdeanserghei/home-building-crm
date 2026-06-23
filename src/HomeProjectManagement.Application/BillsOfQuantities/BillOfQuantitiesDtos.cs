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
    MoneyDto TotalWithVat,
    IReadOnlyList<SectionDto> Sections,
    string? SourceContentHash,
    SourceDocumentDto? SourceDocument,
    DateTimeOffset CreatedAt);

/// <summary>Provenance reference to the source <c>deviz</c> document (mirrors <c>DocumentReference</c>).</summary>
public sealed record SourceDocumentDto(
    string FileName,
    string Url,
    DateTimeOffset UploadedOn,
    Guid UploadedBy);

/// <summary>
/// A section of the BoQ with its directly-held line items and its subsections (mirrors the
/// <c>Section</c> entity). <c>Subtotal</c> covers both the direct lines and the subsections.
/// </summary>
public sealed record SectionDto(
    Guid Id,
    string Name,
    int Sequence,
    string? Description,
    MoneyDto Subtotal,
    MoneyDto SubtotalWithVat,
    IReadOnlyList<LineItemDto> LineItems,
    IReadOnlyList<SubsectionDto> Subsections);

/// <summary>A subsection of a section with its line items (mirrors the <c>Subsection</c> entity).</summary>
public sealed record SubsectionDto(
    Guid Id,
    string Name,
    int Sequence,
    string? Description,
    MoneyDto Subtotal,
    MoneyDto SubtotalWithVat,
    IReadOnlyList<LineItemDto> LineItems);

/// <summary>
/// A priced line within a section (mirrors the <c>LineItem</c> entity). <c>UnitPrice</c> and
/// <c>LineTotal</c> are net (VAT-exclusive); <c>VatRatePercentage</c> (21 by default) yields the
/// derived gross <c>UnitPriceWithVat</c> and <c>LineTotalWithVat</c>.
/// </summary>
public sealed record LineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    MoneyDto UnitPrice,
    decimal VatRatePercentage,
    MoneyDto UnitPriceWithVat,
    MoneyDto LineTotal,
    MoneyDto LineTotalWithVat,
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
    DateTimeOffset? ValidUntil,
    string? SourceContentHash = null,
    string? SourceDocumentFileName = null,
    string? SourceDocumentUrl = null);

/// <summary>
/// One raw line for the <b>bulk</b> add operation: the unit arrives as a free-text token
/// (e.g. "mc", "m³", "buc") which the application service normalises onto an active canonical
/// <c>UnitOfMeasure</c> via its code/aliases. <see cref="VatRatePercentage"/> defaults to 21% when null.
/// </summary>
public sealed record BoqLineItemInput(
    string Description,
    string Unit,
    decimal Quantity,
    MoneyDto UnitPrice,
    decimal? VatRatePercentage = null,
    string? Notes = null);

/// <summary>
/// Outcome of a bulk add: the updated BoQ (with every resolvable line persisted) plus the lines
/// whose unit token could not be matched to an active unit of measure. A single unresolved unit
/// does not fail the batch — those lines are reported back, flagged with the offending token, for an
/// admin to add the missing unit. <see cref="Boq"/> is null only when the BoQ or section was not found.
/// </summary>
public sealed record AddBoqLineItemsResult(
    BillOfQuantitiesDto? Boq,
    IReadOnlyList<UnresolvedBoqLine> Unresolved);

/// <summary>A bulk line that was rejected because its unit token matched no active unit of measure.</summary>
public sealed record UnresolvedBoqLine(int Index, string Description, string Unit);

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

/// <summary>Input for adding or editing a subsection (same shape as a section heading).</summary>
public sealed record SubsectionCommand(string Name, int Sequence, string? Description);

/// <summary>
/// Input for adding or editing a line item. The unit price (net, VAT-exclusive) must be in the
/// BoQ's pricing currency, and the unit of measure must reference an active canonical unit.
/// <see cref="VatRatePercentage"/> is the VAT rate as a percentage (e.g. 21 for 21%); when omitted
/// (null) the standard 21% rate is applied.
/// </summary>
public sealed record LineItemCommand(
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    MoneyDto UnitPrice,
    decimal? VatRatePercentage,
    int Sequence,
    string? Notes);
