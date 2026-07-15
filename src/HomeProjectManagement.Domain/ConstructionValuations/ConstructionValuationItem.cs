using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Domain.ConstructionValuations;

/// <summary>
/// One assessed row of a dated <see cref="ConstructionValuation"/> snapshot: the appraiser's completion
/// percentage for a catalog item, plus the completed and remaining values. Every money field is
/// <b>computed once at capture and then frozen</b> — reads never recompute.
/// </summary>
/// <remarks>
/// A local entity inside the <see cref="ConstructionValuation"/> aggregate. It keeps the
/// <see cref="ValuationCatalogItemId"/> only to group the same item across snapshots and to reach that
/// item's BoQ links; the name and estimate are <b>denormalized</b> from the catalog item at capture so a
/// snapshot renders itself without joining a catalog that may since have changed (and survives the item's
/// later deactivation). This is the deliberate asymmetry with <see cref="ValuationCatalogItem"/>, whose
/// gross total <i>is</i> recomputed on a VAT change.
/// </remarks>
public sealed class ConstructionValuationItem : Entity<ConstructionValuationItemId>
{
    /// <summary>The catalog item assessed (by id) — for grouping across snapshots and reaching its BoQ links.</summary>
    public ValuationCatalogItemId ValuationCatalogItemId { get; private set; }

    /// <summary>Denormalized from the catalog item at capture.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Catalog <c>TotalCostWithoutVat</c> at capture (frozen).</summary>
    public Money EstimatedValueWithoutVat { get; private set; } = null!;

    /// <summary>Catalog <c>TotalCostWithVat</c> at capture (frozen).</summary>
    public Money EstimatedValueWithVat { get; private set; } = null!;

    /// <summary>% executat (col H) — the appraiser's number, 0..100.</summary>
    public decimal CompletionPercentage { get; private set; }

    /// <summary>lei executați, fără TVA (col I, frozen).</summary>
    public Money CompletedValueWithoutVat { get; private set; } = null!;

    /// <summary>lei executați, cu TVA (col I incl. VAT, frozen).</summary>
    public Money CompletedValueWithVat { get; private set; } = null!;

    /// <summary>% rămas (col J = 100 − H, frozen).</summary>
    public decimal RemainingPercentage { get; private set; }

    /// <summary>lei rămași, fără TVA (col K, frozen).</summary>
    public Money RemainingValueWithoutVat { get; private set; } = null!;

    /// <summary>lei rămași, cu TVA (col K incl. VAT, frozen).</summary>
    public Money RemainingValueWithVat { get; private set; } = null!;

    // EF Core materialisation constructor.
    private ConstructionValuationItem()
    {
    }

    // Created only by the ConstructionValuation root (see ConstructionValuation.AddAssessedItem).
    // The completed/remaining values are derived from the frozen estimate and the completion % here, once,
    // then never recomputed — the whole point of a snapshot being a frozen historical fact.
    internal ConstructionValuationItem(
        ConstructionValuationItemId id,
        ValuationCatalogItemId valuationCatalogItemId,
        string name,
        Money estimatedValueWithoutVat,
        Money estimatedValueWithVat,
        decimal completionPercentage)
        : base(id)
    {
        ValuationCatalogItemId = valuationCatalogItemId;
        Name = NormalizeName(name);
        EstimatedValueWithoutVat = estimatedValueWithoutVat;
        EstimatedValueWithVat = estimatedValueWithVat;

        CompletionPercentage = EnsurePercentage(completionPercentage);
        RemainingPercentage = 100m - CompletionPercentage;

        var completedFraction = CompletionPercentage / 100m;
        var remainingFraction = RemainingPercentage / 100m;

        CompletedValueWithoutVat = estimatedValueWithoutVat.Multiply(completedFraction);
        CompletedValueWithVat = estimatedValueWithVat.Multiply(completedFraction);
        RemainingValueWithoutVat = estimatedValueWithoutVat.Multiply(remainingFraction);
        RemainingValueWithVat = estimatedValueWithVat.Multiply(remainingFraction);
    }

    private static decimal EnsurePercentage(decimal percentage)
    {
        if (percentage is < 0m or > 100m)
        {
            throw new DomainValidationException(
                "Completion percentage must be between 0 and 100.",
                nameof(percentage),
                code: "CompletionPercentageOutOfRange");
        }

        return percentage;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException(
                "Construction valuation item name is required.", nameof(name));
        }

        return name.Trim();
    }
}
