using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.ConstructionValuations;

/// <summary>
/// Read model for one dated, frozen site-visit snapshot. Every money field was computed once at capture
/// and is projected straight from storage — reads never recompute. <c>RonPerEur</c> is derived from the
/// snapshot's pinned rate so the UI's EUR toggle works. <c>Totals</c> roll the items up per currency
/// (money never sums across currencies) plus an approximate EUR equivalent from the pinned rate.
/// </summary>
public sealed record ConstructionValuationDto(
    Guid Id,
    Guid ValuationCatalogId,
    DateOnly AssessedOn,
    string? Appraiser,
    ExchangeRateDto ExchangeRate,
    decimal RonPerEur,
    SourceDocumentDto? SourceDocument,
    string? SourceContentHash,
    IReadOnlyList<ConstructionValuationItemDto> Items,
    ValuationProgressTotalsDto Totals,
    DateTimeOffset CreatedAt);

/// <summary>One frozen assessed row (mirrors the <c>ConstructionValuationItem</c> entity).</summary>
public sealed record ConstructionValuationItemDto(
    Guid Id,
    Guid ValuationCatalogItemId,
    string Name,
    MoneyDto EstimatedValueWithoutVat,
    MoneyDto EstimatedValueWithVat,
    decimal CompletionPercentage,
    MoneyDto CompletedValueWithoutVat,
    MoneyDto CompletedValueWithVat,
    decimal RemainingPercentage,
    MoneyDto RemainingValueWithoutVat,
    MoneyDto RemainingValueWithVat);

/// <summary>Per-currency progress totals plus an approximate EUR equivalent (from the snapshot's pinned rate).</summary>
public sealed record ValuationProgressTotalsDto(
    IReadOnlyList<ValuationCurrencyTotalDto> ByCurrency,
    ValuationEurEquivalentDto? EurEquivalent);

/// <summary>Estimated / completed / remaining totals for a single currency (net and gross).</summary>
public sealed record ValuationCurrencyTotalDto(
    Currency Currency,
    MoneyDto EstimatedWithoutVat,
    MoneyDto EstimatedWithVat,
    MoneyDto CompletedWithoutVat,
    MoneyDto CompletedWithVat,
    MoneyDto RemainingWithoutVat,
    MoneyDto RemainingWithVat);

/// <summary>
/// The gross (VAT-inclusive) totals converted to a single comparable EUR figure using the snapshot's
/// pinned <c>RonPerEur</c> rate ("1 EUR = N RON"). Approximate; the pinned rate is the source of truth.
/// </summary>
public sealed record ValuationEurEquivalentDto(
    decimal RonPerEur,
    MoneyDto EstimatedWithVat,
    MoneyDto CompletedWithVat,
    MoneyDto RemainingWithVat);

/// <summary>
/// Input for capturing a snapshot against a catalog. The catalog comes from the route. Each item supplies
/// only the appraiser's completion % keyed by catalog item id — the estimated/completed/remaining money
/// values are derived server-side from the catalog item's current totals at capture and then frozen.
/// Re-capturing with the same <see cref="SourceContentHash"/> returns the existing snapshot (idempotent).
/// </summary>
public sealed record CaptureConstructionValuationCommand(
    DateOnly AssessedOn,
    ExchangeRateDto ExchangeRate,
    IReadOnlyList<AssessedItemInput> Items,
    string? Appraiser = null,
    string? SourceContentHash = null,
    string? SourceDocumentFileName = null,
    string? SourceDocumentUrl = null);

/// <summary>One assessed row's input: which catalog item, and the appraiser's completion % (0..100).</summary>
public sealed record AssessedItemInput(Guid ValuationCatalogItemId, decimal CompletionPercentage);
