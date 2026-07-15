using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// One priced row of the appraiser's estimate (a <i>lucrare</i> in the <i>fișă de calcul</i>): a name,
/// the printed unit and catalog source, the cost weight, and the total value with/without VAT. Owns the
/// set of <see cref="ValuationItemLink"/>s that map it to the owners' real BoQ sections.
/// </summary>
/// <remarks>
/// A local entity inside the <see cref="ValuationCatalog"/> aggregate — created, revised, and retired
/// only through the root. Retired via <see cref="Deactivate"/> (never hard-deleted) because
/// <see cref="ConstructionValuations"/> snapshots reference it by id. Its <see cref="TotalCostWithVat"/>
/// is a <b>stored</b> value refreshed by <see cref="RecomputeVat"/> when the catalog's VAT rate changes —
/// contrast the snapshot item, which recomputes nothing.
/// </remarks>
public sealed class ValuationCatalogItem : Entity<ValuationCatalogItemId>
{
    private readonly List<ValuationItemLink> _links = [];

    /// <summary>Stable display order within the catalog.</summary>
    public int Sequence { get; private set; }

    /// <summary>The appraiser's printed <i>Nr. Crt.</i> verbatim (quirks preserved, e.g. a duplicated "25").</summary>
    public string PrintedNumber { get; private set; } = null!;

    /// <summary>Denumirea lucrării (e.g. "Beton armat în structură").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>UM as printed — raw text (<c>mc</c>/<c>mp</c>/<c>ml</c>/<c>kg</c>, or lump-sum markers
    /// <c>%</c>/<c>lei</c>); deliberately <b>not</b> linked to the <c>UnitOfMeasure</c> vocabulary.</summary>
    public string Unit { get; private set; } = null!;

    /// <summary>Sursa / Nr. Fișă (e.g. <c>F.38</c>, <c>Deviz</c>, <c>F.26</c>, <c>F.24</c>).</summary>
    public string CatalogSource { get; private set; } = null!;

    /// <summary>Pondere în total cost (the item's share of the total).</summary>
    public decimal CostWeight { get; private set; }

    /// <summary>Cost lucrare (Lei/mpAd) — unit cost per built area.</summary>
    public Money UnitCostPerBuiltArea { get; private set; } = null!;

    /// <summary>Cost total, fără TVA (col G) — the VAT-exclusive estimate compared against real BoQ cost.</summary>
    public Money TotalCostWithoutVat { get; private set; } = null!;

    /// <summary>Cost total, cu TVA — <b>stored</b>, kept in sync by <see cref="RecomputeVat"/>.</summary>
    public Money TotalCostWithVat { get; private set; } = null!;

    /// <summary>Whether the item is live; retired via <see cref="Deactivate"/> rather than deleted.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The BoQ sections this item maps to (owned value objects). Mutated only via the catalog root.</summary>
    public IReadOnlyList<ValuationItemLink> Links => _links.AsReadOnly();

    // EF Core materialisation constructor.
    private ValuationCatalogItem()
    {
    }

    // Created only by the ValuationCatalog root (see ValuationCatalog.AddItem).
    internal ValuationCatalogItem(
        ValuationCatalogItemId id,
        int sequence,
        string printedNumber,
        string name,
        string unit,
        string catalogSource,
        decimal costWeight,
        Money unitCostPerBuiltArea,
        Money totalCostWithoutVat,
        VatRate vatRate)
        : base(id)
    {
        Sequence = sequence;
        PrintedNumber = NormalizePrintedNumber(printedNumber);
        Name = NormalizeName(name);
        Unit = NormalizeUnit(unit);
        CatalogSource = NormalizeCatalogSource(catalogSource);
        CostWeight = costWeight;
        UnitCostPerBuiltArea = unitCostPerBuiltArea;
        TotalCostWithoutVat = totalCostWithoutVat;
        TotalCostWithVat = vatRate.ApplyTo(totalCostWithoutVat);
        IsActive = true;
    }

    /// <summary>Update the item's priced fields, recomputing <see cref="TotalCostWithVat"/> from the catalog's VAT rate.</summary>
    internal void Revise(
        int sequence,
        string printedNumber,
        string name,
        string unit,
        string catalogSource,
        decimal costWeight,
        Money unitCostPerBuiltArea,
        Money totalCostWithoutVat,
        VatRate vatRate)
    {
        Sequence = sequence;
        PrintedNumber = NormalizePrintedNumber(printedNumber);
        Name = NormalizeName(name);
        Unit = NormalizeUnit(unit);
        CatalogSource = NormalizeCatalogSource(catalogSource);
        CostWeight = costWeight;
        UnitCostPerBuiltArea = unitCostPerBuiltArea;
        TotalCostWithoutVat = totalCostWithoutVat;
        TotalCostWithVat = vatRate.ApplyTo(totalCostWithoutVat);
    }

    /// <summary>Retire the item without deleting it (snapshots still reference it by id).</summary>
    internal void Deactivate() => IsActive = false;

    /// <summary>Refresh the stored gross total for a new catalog VAT rate (write-time recompute).</summary>
    internal void RecomputeVat(VatRate vatRate) => TotalCostWithVat = vatRate.ApplyTo(TotalCostWithoutVat);

    internal bool HasLink(ValuationItemLink link) => _links.Contains(link);

    internal void AddLink(ValuationItemLink link)
    {
        if (!_links.Contains(link))
        {
            _links.Add(link);
        }
    }

    internal bool RemoveLink(ValuationItemLink link) => _links.Remove(link);

    private static string NormalizePrintedNumber(string printedNumber)
    {
        if (string.IsNullOrWhiteSpace(printedNumber))
        {
            throw new DomainValidationException(
                "Valuation catalog item printed number is required.", nameof(printedNumber));
        }

        return printedNumber.Trim();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException(
                "Valuation catalog item name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new DomainValidationException(
                "Valuation catalog item unit is required.", nameof(unit));
        }

        return unit.Trim();
    }

    private static string NormalizeCatalogSource(string catalogSource)
    {
        if (string.IsNullOrWhiteSpace(catalogSource))
        {
            throw new DomainValidationException(
                "Valuation catalog item source is required.", nameof(catalogSource));
        }

        return catalogSource.Trim();
    }
}
