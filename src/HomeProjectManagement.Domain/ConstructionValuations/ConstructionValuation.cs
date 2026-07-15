using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.ConstructionValuations.Events;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Domain.ConstructionValuations;

/// <summary>
/// A single dated site-visit assessment against a <see cref="ValuationCatalog"/>: "on this visit, item X
/// is 40% done". Many per catalog. A <b>frozen historical fact</b> — its item values are computed once at
/// capture and never recomputed on read, so later catalog edits do not rewrite a past assessment.
/// </summary>
/// <remarks>
/// Aggregate root. References its <see cref="ValuationCatalogId"/> <b>by identity</b> and owns a flat list
/// of <see cref="ConstructionValuationItem"/>s. Import is idempotent by <see cref="SourceContentHash"/>
/// (the same content maps to the same snapshot — the check lives in the application service, backed by the
/// repository lookup). The pinned <see cref="ExchangeRate"/> lives here, not on the catalog, because the
/// RON/EUR rate moves between visits. Callers pass <c>DateTimeOffset now</c>; the domain reads no clock.
/// </remarks>
public sealed class ConstructionValuation : AggregateRoot<ConstructionValuationId>
{
    private readonly List<ConstructionValuationItem> _items = [];

    /// <summary>The catalog this snapshot assesses (by id).</summary>
    public ValuationCatalogId ValuationCatalogId { get; private set; }

    /// <summary>The site-visit / assessment date.</summary>
    public DateOnly AssessedOn { get; private set; }

    /// <summary>Appraiser name/firm. Optional.</summary>
    public string? Appraiser { get; private set; }

    /// <summary>RON/EUR rate pinned for this report (moves between visits).</summary>
    public ExchangeRate ExchangeRate { get; private set; } = null!;

    /// <summary>The source report (agent-provided). Optional.</summary>
    public DocumentReference? SourceDocument { get; private set; }

    /// <summary>Content hash for idempotent import. Optional.</summary>
    public string? SourceContentHash { get; private set; }

    /// <summary>The frozen assessed rows (internal entities). Populated at capture; never recomputed on read.</summary>
    public IReadOnlyList<ConstructionValuationItem> Items => _items.AsReadOnly();

    // EF Core materialisation constructor.
    private ConstructionValuation()
    {
    }

    private ConstructionValuation(
        ConstructionValuationId id,
        ValuationCatalogId valuationCatalogId,
        DateOnly assessedOn,
        ExchangeRate exchangeRate) : base(id)
    {
        ValuationCatalogId = valuationCatalogId;
        AssessedOn = assessedOn;
        ExchangeRate = exchangeRate;
    }

    /// <summary>
    /// Factory: begin a new snapshot against a catalog. Assessed items are then added via
    /// <see cref="AddAssessedItem"/> (typically by an application service looping the catalog's active
    /// items and feeding each estimate + the appraiser's completion %).
    /// </summary>
    public static ConstructionValuation Capture(
        ValuationCatalogId valuationCatalogId,
        DateOnly assessedOn,
        ExchangeRate exchangeRate,
        DateTimeOffset now,
        string? appraiser = null,
        DocumentReference? sourceDocument = null,
        string? sourceContentHash = null)
    {
        var valuation = new ConstructionValuation(
            ConstructionValuationId.New(), valuationCatalogId, assessedOn, exchangeRate)
        {
            Appraiser = Trim(appraiser),
            SourceDocument = sourceDocument,
            SourceContentHash = Trim(sourceContentHash)
        };

        valuation.Raise(new ConstructionValuationCaptured(valuation.Id, valuationCatalogId, assessedOn, now));
        return valuation;
    }

    /// <summary>
    /// Freeze one assessed row: the domain derives completed/remaining from the supplied estimate and the
    /// appraiser's completion %, then stores them. There is no method to recompute the row afterwards —
    /// the deliberate asymmetry with <see cref="ValuationCatalog.ChangeVatRate"/>.
    /// </summary>
    public ConstructionValuationItem AddAssessedItem(
        ValuationCatalogItemId valuationCatalogItemId,
        string name,
        Money estimatedValueWithoutVat,
        Money estimatedValueWithVat,
        decimal completionPercentage)
    {
        var item = new ConstructionValuationItem(
            ConstructionValuationItemId.New(),
            valuationCatalogItemId,
            name,
            estimatedValueWithoutVat,
            estimatedValueWithVat,
            completionPercentage);

        _items.Add(item);
        return item;
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
