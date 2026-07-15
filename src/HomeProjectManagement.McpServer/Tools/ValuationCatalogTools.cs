using System.ComponentModel;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.ValuationCatalogs;
using HomeProjectManagement.Application.Valuations;
using HomeProjectManagement.Domain.Common.ValueObjects;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The appraiser's valuation-catalog surface (the <c>fișă de calcul</c> baseline). The agent reads the
/// appraiser's spreadsheet — the itemized estimate priced from a standard catalog — and emits structured
/// data; these tools validate and persist it. The server never parses the file. A project has at most one
/// catalog; it is edited in place. Tools also map each catalog item onto the owners' real BoQ sections and
/// expose the live estimate-vs-real comparison. Thin wrappers over <see cref="IValuationCatalogAppService"/>
/// (mutations) and <see cref="IValuationVsBoqQuery"/> (the computed comparison).
/// </summary>
[McpServerToolType]
public static class ValuationCatalogTools
{
    /// <summary>One priced estimate row as the agent read it from the appraiser's sheet.</summary>
    public sealed record ValuationItemRow(
        [property: Description("The appraiser's printed Nr. Crt. verbatim (quirks included — the sheet may duplicate or skip numbers).")] string PrintedNumber,
        [property: Description("The work name (Denumirea lucrării), e.g. \"Beton armat în structură\".")] string Name,
        [property: Description("Unit as printed (raw text — mc/mp/ml/kg, or a lump-sum marker like % / lei / RON). Not normalized.")] string Unit,
        [property: Description("Catalog source of the price, e.g. F.38 / Deviz / F.26 / F.24.")] string CatalogSource,
        [property: Description("Cost weight (pondere în total cost), as a fraction.")] decimal CostWeight,
        [property: Description("Unit cost per built area (Lei/mpAd), in the catalog currency.")] decimal UnitCostPerBuiltArea,
        [property: Description("Total cost without VAT (col G), in the catalog currency.")] decimal TotalCostWithoutVat,
        [property: Description("Optional explicit order; assigned automatically (max + 1) when omitted.")] int? Sequence = null);

    [McpServerTool(Name = "get_project_valuation_catalog"), Description(
        "Get a project's valuation catalog (the appraiser's fișă de calcul baseline), or null if none has " +
        "been created yet. Use this first to discover whether a project already has a catalog — and its id — " +
        "before creating one (there is at most one catalog per project). Resolve the projectId via list_projects.")]
    public static async Task<ValuationCatalogDto?> GetProjectValuationCatalog(
        IValuationCatalogAppService service,
        [Description("The project whose catalog to fetch.")] Guid projectId,
        CancellationToken ct = default)
        => await service.GetByProjectAsync(projectId, ct);

    [McpServerTool(Name = "get_valuation_catalog"), Description(
        "Read a valuation catalog back by its id — header, priced items (with sequences and totals), and each " +
        "item's BoQ links — to verify an ingestion.")]
    public static async Task<ValuationCatalogDto> GetValuationCatalog(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        CancellationToken ct = default)
        => await service.GetAsync(catalogId, ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "create_valuation_catalog"), Description(
        "Create the valuation catalog for a project from the appraiser's report header (call " +
        "get_project_valuation_catalog first — a project has at most one catalog, and a second is rejected). " +
        "Supply the report currency (RON), VAT rate, the three surfaces (built / gross-floor SCD / usable), " +
        "and the own-regie adjustment. Starts in Draft; add priced items with add_valuation_items, then " +
        "activate_valuation_catalog. Returns the created catalog including its id.")]
    public static async Task<ValuationCatalogDto> CreateValuationCatalog(
        IValuationCatalogAppService service,
        [Description("The project the catalog belongs to.")] Guid projectId,
        [Description("Catalog reference, e.g. \"MATRIX, Fișa 38\".")] string catalogReference,
        [Description("Report currency (RON or EUR); all item money is in it.")] Currency currency,
        [Description("VAT rate percentage (e.g. 21).")] decimal vatRatePercentage,
        [Description("Built area used to price per-built-area costs.")] decimal builtArea,
        [Description("Gross floor area (SCD).")] decimal grossFloorArea,
        [Description("Usable area.")] decimal usableArea,
        [Description("Own-regie adjustment (e.g. 0.20), stored for provenance.")] decimal ownRegieAdjustment,
        CancellationToken ct = default)
        => await service.CreateAsync(
               projectId,
               new CreateValuationCatalogCommand(
                   catalogReference, currency, vatRatePercentage,
                   builtArea, grossFloorArea, usableArea, ownRegieAdjustment),
               ct)
           ?? throw new McpException($"No project exists with id {projectId}.");

    [McpServerTool(Name = "update_valuation_catalog_header"), Description(
        "Edit a catalog's header in place: the catalog reference, the three surfaces (built / gross-floor SCD " +
        "/ usable), and the own-regie adjustment. The currency and VAT rate are not changed here (use " +
        "change_valuation_vat_rate for VAT). Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> UpdateValuationCatalogHeader(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("Catalog reference, e.g. \"MATRIX, Fișa 38\".")] string catalogReference,
        [Description("Built area.")] decimal builtArea,
        [Description("Gross floor area (SCD).")] decimal grossFloorArea,
        [Description("Usable area.")] decimal usableArea,
        [Description("Own-regie adjustment (e.g. 0.20).")] decimal ownRegieAdjustment,
        CancellationToken ct = default)
        => await service.UpdateHeaderAsync(
               catalogId,
               new UpdateValuationCatalogHeaderCommand(
                   catalogReference, builtArea, grossFloorArea, usableArea, ownRegieAdjustment),
               ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "activate_valuation_catalog"), Description(
        "Activate a Draft catalog (Draft → Active) once its items are entered. Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> ActivateValuationCatalog(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        CancellationToken ct = default)
        => await service.ActivateAsync(catalogId, ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "change_valuation_vat_rate"), Description(
        "Change the catalog's VAT rate. The catalog recomputes each item's stored gross total (with VAT) on " +
        "its current net values — a write-time recompute. It does NOT touch existing captured snapshots " +
        "(those are frozen historical facts). Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> ChangeValuationVatRate(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The new VAT rate percentage (e.g. 21).")] decimal percentage,
        CancellationToken ct = default)
        => await service.ChangeVatRateAsync(catalogId, new ChangeVatRateCommand(percentage), ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "add_valuation_items"), Description(
        "Bulk-add priced items to a catalog — the whole itemized estimate in one call. Each row's money " +
        "(unit cost, total without VAT) is taken to be in the catalog's currency; the gross total is computed " +
        "from the catalog's current VAT rate. The unit is stored as raw text (mc/mp/%/lei) — it is NOT " +
        "normalized against a unit vocabulary, unlike a BoQ. Sequence is assigned automatically (max + 1) when " +
        "omitted; printedNumber preserves the appraiser's Nr. Crt. verbatim. Returns the updated catalog; read " +
        "each item's id from it before linking BoQ sections or capturing a snapshot.")]
    public static async Task<ValuationCatalogDto> AddValuationItems(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The priced items to add.")] IReadOnlyList<ValuationItemRow> items,
        CancellationToken ct = default)
    {
        var catalog = await service.GetAsync(catalogId, ct)
                      ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

        var nextSequence = catalog.Items.Count == 0 ? 1 : catalog.Items.Max(i => i.Sequence) + 1;

        ValuationCatalogDto? updated = catalog;
        foreach (var item in items)
        {
            updated = await service.AddItemAsync(
                catalogId,
                new AddValuationItemCommand(
                    item.Sequence ?? nextSequence++,
                    item.PrintedNumber,
                    item.Name,
                    item.Unit,
                    item.CatalogSource,
                    item.CostWeight,
                    new MoneyDto(item.UnitCostPerBuiltArea, catalog.Currency),
                    new MoneyDto(item.TotalCostWithoutVat, catalog.Currency)),
                ct);
        }

        return updated ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");
    }

    [McpServerTool(Name = "revise_valuation_item"), Description(
        "Correct a single priced item on a catalog. Provide all fields (same shape as add_valuation_items). " +
        "Money is taken to be in the catalog's currency; the gross total recomputes from the catalog's VAT " +
        "rate. Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> ReviseValuationItem(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The item id to revise.")] Guid itemId,
        [Description("The appraiser's printed Nr. Crt. verbatim.")] string printedNumber,
        [Description("The work name.")] string name,
        [Description("Unit as printed (raw text).")] string unit,
        [Description("Catalog source (F.38 / Deviz / …).")] string catalogSource,
        [Description("Cost weight, as a fraction.")] decimal costWeight,
        [Description("Unit cost per built area, in the catalog currency.")] decimal unitCostPerBuiltArea,
        [Description("Total cost without VAT, in the catalog currency.")] decimal totalCostWithoutVat,
        [Description("Order of the item within the catalog.")] int sequence,
        CancellationToken ct = default)
    {
        var catalog = await service.GetAsync(catalogId, ct)
                      ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

        var command = new ReviseValuationItemCommand(
            sequence, printedNumber, name, unit, catalogSource, costWeight,
            new MoneyDto(unitCostPerBuiltArea, catalog.Currency),
            new MoneyDto(totalCostWithoutVat, catalog.Currency));

        return await service.ReviseItemAsync(catalogId, itemId, command, ct)
               ?? throw new McpException($"Catalog {catalogId} or item {itemId} was not found.");
    }

    [McpServerTool(Name = "deactivate_valuation_item"), Description(
        "Retire an item from the catalog (soft-delete). It is deactivated rather than hard-deleted because " +
        "captured snapshots reference it by id. Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> DeactivateValuationItem(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The item id to deactivate.")] Guid itemId,
        CancellationToken ct = default)
        => await service.DeactivateItemAsync(catalogId, itemId, ct)
           ?? throw new McpException($"Catalog {catalogId} or item {itemId} was not found.");

    [McpServerTool(Name = "link_valuation_item_to_boq"), Description(
        "Map a catalog item onto a real BoQ target so its estimate can be compared to the owners' actual cost. " +
        "Provide the boqId and a target at one of three granularities: a sectionId (map the whole section), a " +
        "subsectionId (map just that subsection), or a lineItemId (map a single line). For the finer levels the " +
        "parent section/subsection is resolved server-side; the work package is derived from the BoQ. Two " +
        "invariants are enforced: (1) no double-counting — a given BoQ section/subsection/line is linked to at " +
        "most one item; (2) granularity exclusivity — within one section the three levels are nested and " +
        "mutually exclusive (Section covers Subsection covers Line): map a section as a whole, OR " +
        "subsection-by-subsection, OR line-by-line, never a coarser and a finer target together (switch by " +
        "unlinking first). Discover section/subsection/line ids with get_boq. Returns the updated catalog.")]
    public static async Task<ValuationCatalogDto> LinkValuationItemToBoq(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The catalog item id to map.")] Guid itemId,
        [Description("The BoQ id (from get_bid_boq / get_boq).")] Guid boqId,
        [Description("The BoQ section id to map as a whole. Omit when mapping a subsection or line.")] Guid? sectionId = null,
        [Description("The BoQ subsection id to map. Omit when mapping the whole section or a single line.")] Guid? subsectionId = null,
        [Description("The BoQ line item id to map a single line. Omit when mapping a whole section or subsection.")] Guid? lineItemId = null,
        CancellationToken ct = default)
        => await service.LinkBoqSectionAsync(
               catalogId, itemId, new LinkBoqSectionCommand(boqId, sectionId, subsectionId, lineItemId), ct)
           ?? throw new McpException($"Catalog {catalogId} or item {itemId} was not found.");

    [McpServerTool(Name = "unlink_valuation_item_from_boq"), Description(
        "Remove a BoQ mapping from a catalog item (the inverse of link_valuation_item_to_boq). Identify the " +
        "same target: boqId plus the sectionId (whole-section link), the subsectionId (subsection link), or the " +
        "lineItemId (line link). Do this before switching a section's mapping granularity. Returns the updated " +
        "catalog.")]
    public static async Task<ValuationCatalogDto> UnlinkValuationItemFromBoq(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        [Description("The catalog item id.")] Guid itemId,
        [Description("The BoQ id of the link to remove.")] Guid boqId,
        [Description("The section id of the link (its real parent for a subsection/line link).")] Guid? sectionId = null,
        [Description("The subsection id of a subsection link, or a line link's parent subsection. Omit otherwise.")] Guid? subsectionId = null,
        [Description("The line item id of a line link. Omit for a whole-section or subsection link.")] Guid? lineItemId = null,
        CancellationToken ct = default)
        => await service.UnlinkBoqSectionAsync(
               catalogId, itemId, new LinkBoqSectionCommand(boqId, sectionId, subsectionId, lineItemId), ct)
           ?? throw new McpException($"Catalog {catalogId} or item {itemId} was not found.");

    [McpServerTool(Name = "delete_valuation_catalog"), Description(
        "Delete a project's valuation catalog (and its items/links). Returns a confirmation message.")]
    public static async Task<string> DeleteValuationCatalog(
        IValuationCatalogAppService service,
        [Description("The catalog id.")] Guid catalogId,
        CancellationToken ct = default)
        => await service.DeleteAsync(catalogId, ct)
            ? $"Deleted valuation catalog {catalogId}."
            : throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "get_valuation_vs_boq"), Description(
        "The live estimate-vs-real comparison for a project's catalog: per item, the appraiser's estimate vs. " +
        "the summed real cost of its linked BoQ sections, with the absolute and % variance. Items with no " +
        "links, and BoQ lines held directly in a section that only has subsection links, surface as coverage " +
        "gaps (with a coverage percentage) rather than distorting the variance. Competing BoQs of a work " +
        "package collapse to one 'real' BoQ; a work package with none realized is reported as NotRealized, not " +
        "−100%. Basis: omit costScenarioId for the default 'Decided' basis (the accepted contract's BoQ, else " +
        "the selected bid's); pass a costScenarioId (from list_cost_scenarios) to compare against that " +
        "scenario's chosen bids instead. Throws if the project has no catalog.")]
    public static async Task<ValuationVsBoqDto> GetValuationVsBoq(
        IValuationVsBoqQuery query,
        [Description("The project whose comparison to compute.")] Guid projectId,
        [Description("Optional cost scenario to use as the 'real' basis; omit for the decided basis.")] Guid? costScenarioId = null,
        CancellationToken ct = default)
    {
        var comparison = costScenarioId is { } scenarioId
            ? await query.GetAsync(projectId, new ComparisonBasis.Scenario(scenarioId), ct)
            : await query.GetByProjectAsync(projectId, ct);

        return comparison
               ?? throw new McpException($"No valuation catalog exists for project {projectId}.");
    }
}
