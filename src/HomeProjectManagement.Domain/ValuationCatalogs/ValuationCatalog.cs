using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs.Events;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// The bank appraiser's itemized construction estimate for a project (segregated-cost method), plus the
/// mapping of each item to the owners' real bill-of-quantities sections. The slow-moving baseline —
/// distinct from a contractor's <c>deviz</c> (real cost) and from the dated completion assessments hung
/// off it.
/// </summary>
/// <remarks>
/// Aggregate root. References its owning <see cref="ProjectId"/> <b>by identity</b> and owns a flat list
/// of <see cref="ValuationCatalogItem"/>s, each owning its <see cref="ValuationItemLink"/>s. Editable in
/// place — a revised report edits the catalog rather than versioning it; historical fidelity comes from
/// the frozen <c>ConstructionValuation</c> snapshots. Callers pass <c>DateTimeOffset now</c>; the domain
/// reads no clock. One catalog per project is enforced outside the domain (a DB unique index on
/// <c>projectId</c> plus an application-service check) because a root cannot see sibling catalogs.
/// </remarks>
public sealed class ValuationCatalog : AggregateRoot<ValuationCatalogId>
{
    private readonly List<ValuationCatalogItem> _items = [];

    /// <summary>The owning project (by id). Unique across catalogs — one per project.</summary>
    public ProjectId ProjectId { get; private set; }

    /// <summary>The appraisal method (<see cref="ValuationMethod.SegregatedCost"/>).</summary>
    public ValuationMethod Method { get; private set; }

    /// <summary>The standard-catalog basis, e.g. "MATRIX, Fișa 38".</summary>
    public string CatalogReference { get; private set; } = null!;

    public ValuationCatalogStatus Status { get; private set; }

    /// <summary>Pricing currency of the estimate (RON).</summary>
    public Currency Currency { get; private set; }

    /// <summary>Report VAT (21%); changing it recomputes each item's stored <c>TotalCostWithVat</c>.</summary>
    public VatRate VatRate { get; private set; } = null!;

    /// <summary>Suprafață Construită (mp).</summary>
    public decimal BuiltArea { get; private set; }

    /// <summary>Suprafață Construită Desfășurată / SCD (mp).</summary>
    public decimal GrossFloorArea { get; private set; }

    /// <summary>Suprafață Utilă (mp).</summary>
    public decimal UsableArea { get; private set; }

    /// <summary>Ajustare regie proprie (e.g. 0.20). Stored for provenance.</summary>
    public decimal OwnRegieAdjustment { get; private set; }

    /// <summary>The priced rows (internal entities). Mutated only through this root; EF reaches the backing field.</summary>
    public IReadOnlyList<ValuationCatalogItem> Items => _items.AsReadOnly();

    // EF Core materialisation constructor.
    private ValuationCatalog()
    {
    }

    private ValuationCatalog(ValuationCatalogId id, ProjectId projectId, string catalogReference) : base(id)
    {
        ProjectId = projectId;
        CatalogReference = catalogReference;
    }

    /// <summary>
    /// Factory: create a new draft valuation catalog for a project. The one-catalog-per-project rule is
    /// enforced outside the domain (DB unique index + application-service check).
    /// </summary>
    public static ValuationCatalog Create(
        ProjectId projectId,
        string catalogReference,
        Currency currency,
        VatRate vatRate,
        decimal builtArea,
        decimal grossFloorArea,
        decimal usableArea,
        decimal ownRegieAdjustment,
        DateTimeOffset now,
        ValuationMethod method = ValuationMethod.SegregatedCost)
    {
        var catalog = new ValuationCatalog(
            ValuationCatalogId.New(), projectId, NormalizeCatalogReference(catalogReference))
        {
            Method = method,
            Status = ValuationCatalogStatus.Draft,
            Currency = currency,
            VatRate = vatRate,
            BuiltArea = builtArea,
            GrossFloorArea = grossFloorArea,
            UsableArea = usableArea,
            OwnRegieAdjustment = ownRegieAdjustment
        };

        catalog.Raise(new ValuationCatalogCreated(catalog.Id, projectId, now));
        return catalog;
    }

    /// <summary>Update the catalog's header (reference, surfaces, own-regie adjustment).</summary>
    public void UpdateHeader(
        string catalogReference,
        decimal builtArea,
        decimal grossFloorArea,
        decimal usableArea,
        decimal ownRegieAdjustment)
    {
        CatalogReference = NormalizeCatalogReference(catalogReference);
        BuiltArea = builtArea;
        GrossFloorArea = grossFloorArea;
        UsableArea = usableArea;
        OwnRegieAdjustment = ownRegieAdjustment;
    }

    /// <summary>Add a priced item; its gross total is computed from the catalog's current VAT rate.</summary>
    public ValuationCatalogItem AddItem(
        int sequence,
        string printedNumber,
        string name,
        string unit,
        string catalogSource,
        decimal costWeight,
        Money unitCostPerBuiltArea,
        Money totalCostWithoutVat,
        DateTimeOffset now)
    {
        EnsureCurrency(unitCostPerBuiltArea, nameof(unitCostPerBuiltArea));
        EnsureCurrency(totalCostWithoutVat, nameof(totalCostWithoutVat));

        var item = new ValuationCatalogItem(
            ValuationCatalogItemId.New(),
            sequence,
            printedNumber,
            name,
            unit,
            catalogSource,
            costWeight,
            unitCostPerBuiltArea,
            totalCostWithoutVat,
            VatRate);

        _items.Add(item);
        Raise(new ValuationCatalogItemAdded(Id, item.Id, now));
        return item;
    }

    /// <summary>Revise an existing item's priced fields. Returns false if the item is not in this catalog.</summary>
    public bool ReviseItem(
        ValuationCatalogItemId itemId,
        int sequence,
        string printedNumber,
        string name,
        string unit,
        string catalogSource,
        decimal costWeight,
        Money unitCostPerBuiltArea,
        Money totalCostWithoutVat)
    {
        var item = FindItem(itemId);
        if (item is null)
        {
            return false;
        }

        EnsureCurrency(unitCostPerBuiltArea, nameof(unitCostPerBuiltArea));
        EnsureCurrency(totalCostWithoutVat, nameof(totalCostWithoutVat));

        item.Revise(
            sequence,
            printedNumber,
            name,
            unit,
            catalogSource,
            costWeight,
            unitCostPerBuiltArea,
            totalCostWithoutVat,
            VatRate);
        return true;
    }

    /// <summary>Retire an item (keeps it for snapshot references). Returns false if the item is absent.</summary>
    public bool DeactivateItem(ValuationCatalogItemId itemId)
    {
        var item = FindItem(itemId);
        if (item is null)
        {
            return false;
        }

        item.Deactivate();
        return true;
    }

    /// <summary>
    /// Change the report VAT rate and recompute every item's stored gross total (a write-time recompute
    /// on current state). Past snapshots are deliberately untouched.
    /// </summary>
    public void ChangeVatRate(VatRate vatRate, DateTimeOffset now)
    {
        var previous = VatRate.Percentage;
        if (previous == vatRate.Percentage)
        {
            return;
        }

        VatRate = vatRate;
        foreach (var item in _items)
        {
            item.RecomputeVat(vatRate);
        }

        Raise(new ValuationCatalogVatRateChanged(Id, previous, vatRate.Percentage, now));
    }

    /// <summary>Promote a draft catalog to the project's active baseline. No-op if already active.</summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status == ValuationCatalogStatus.Active)
        {
            return;
        }

        Status = ValuationCatalogStatus.Active;
        Raise(new ValuationCatalogActivated(Id, now));
    }

    /// <summary>
    /// Map a catalog item to a BoQ section/subsection. Enforces two invariants across the whole catalog:
    /// <b>no-double-count</b> — the same <c>(boqId, sectionId, subsectionId)</c> triple may be linked to at
    /// most one item; and <b>granularity exclusivity</b> — for one <c>(boqId, sectionId)</c>, a whole-section
    /// link (<c>subsectionId == null</c>) and its subsection links are mutually exclusive, so a section is
    /// mapped either as a whole (covering all its subsections) or subsection-by-subsection, never both. Both
    /// are checkable from the link tuples alone because a subsection link carries its parent
    /// <c>sectionId</c>. Whether the link points at a real BoQ section — and that a subsection link carries
    /// that subsection's <b>actual</b> parent section — is the application service's responsibility.
    /// </summary>
    public bool LinkBoqSection(ValuationCatalogItemId itemId, ValuationItemLink link)
    {
        var item = FindItem(itemId);
        if (item is null)
        {
            return false;
        }

        if (item.HasLink(link))
        {
            return true;
        }

        var owner = _items.FirstOrDefault(i => i.HasLink(link));
        if (owner is not null)
        {
            throw new DomainConflictException(
                "This BoQ section is already mapped to another valuation catalog item.",
                code: "ValuationLinkAlreadyMapped",
                parameters: new Dictionary<string, object?>
                {
                    ["boqId"] = link.BoqId.Value,
                    ["sectionId"] = link.SectionId.Value,
                    ["subsectionId"] = link.SubsectionId?.Value,
                    ["ownerItemId"] = owner.Id.Value
                });
        }

        EnsureGranularityConsistent(link);

        item.AddLink(link);
        return true;
    }

    // A section is mapped either as a whole or subsection-by-subsection, never both: for one
    // (boqId, sectionId), a whole-section link and any subsection link under it cannot coexist across the
    // catalog. To switch granularity, unlink first. Enforced here — the root sees every item's links.
    private void EnsureGranularityConsistent(ValuationItemLink link)
    {
        var siblings = _items
            .SelectMany(i => i.Links)
            .Where(l => l.BoqId == link.BoqId && l.SectionId == link.SectionId);

        var conflicts = link.SubsectionId is null
            ? siblings.Any(l => l.SubsectionId is not null)   // mapping the whole section, but a subsection is mapped
            : siblings.Any(l => l.SubsectionId is null);      // mapping a subsection, but the whole section is mapped

        if (conflicts)
        {
            throw new DomainConflictException(
                link.SubsectionId is null
                    ? "This BoQ section has subsections mapped individually; unmap them before mapping the whole section."
                    : "This BoQ section is mapped as a whole; unmap the section before mapping its subsections individually.",
                code: "ValuationLinkGranularityConflict",
                parameters: new Dictionary<string, object?>
                {
                    ["boqId"] = link.BoqId.Value,
                    ["sectionId"] = link.SectionId.Value,
                    ["subsectionId"] = link.SubsectionId?.Value
                });
        }
    }

    /// <summary>Remove a BoQ mapping from an item. Returns false if the item or the link is absent.</summary>
    public bool UnlinkBoqSection(ValuationCatalogItemId itemId, ValuationItemLink link)
    {
        var item = FindItem(itemId);
        return item is not null && item.RemoveLink(link);
    }

    private ValuationCatalogItem? FindItem(ValuationCatalogItemId itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId);

    private void EnsureCurrency(Money money, string parameterName)
    {
        if (money.Currency != Currency)
        {
            throw new DomainValidationException(
                $"Catalog item money must be in the catalog currency ({Currency}), not {money.Currency}.",
                parameterName,
                code: "ValuationCatalogCurrencyMismatch");
        }
    }

    private static string NormalizeCatalogReference(string catalogReference)
    {
        if (string.IsNullOrWhiteSpace(catalogReference))
        {
            throw new DomainValidationException(
                "Valuation catalog reference is required.", nameof(catalogReference));
        }

        return catalogReference.Trim();
    }
}
