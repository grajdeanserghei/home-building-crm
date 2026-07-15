using System.ComponentModel;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.ConstructionValuations;
using HomeProjectManagement.Application.Valuations;
using HomeProjectManagement.Domain.Common.ValueObjects;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The dated construction-valuation snapshot surface — the appraiser's on-site completion assessments. The
/// agent reads a site-visit document and emits, per catalog item, the appraiser's completion %; the server
/// derives the completed/remaining money from the catalog's current totals and freezes it. A snapshot is a
/// frozen historical fact — reads never recompute, and later catalog edits do not alter past snapshots.
/// Import is idempotent by source content hash. Thin wrappers over
/// <see cref="IConstructionValuationAppService"/> (capture/read) and <see cref="IValuationProgressQuery"/>
/// (the progress series).
/// </summary>
[McpServerToolType]
public static class ConstructionValuationTools
{
    /// <summary>One assessed row: which catalog item, and the appraiser's completion % (0..100).</summary>
    public sealed record AssessedItemRow(
        [property: Description("The catalog item id being assessed (from get_valuation_catalog).")] Guid ValuationCatalogItemId,
        [property: Description("The appraiser's completion percentage for this item, 0..100 (col % executat).")] decimal CompletionPercentage);

    [McpServerTool(Name = "capture_construction_valuation"), Description(
        "Capture a dated site-visit snapshot against a project's valuation catalog (get_project_valuation_" +
        "catalog / get_valuation_catalog to resolve the catalogId and its item ids). Each row supplies only " +
        "the appraiser's completion % keyed by catalog item id — the estimated / completed / remaining money " +
        "values are derived server-side from each item's current catalog totals at capture and then frozen. " +
        "Pin the RON/EUR rate as of the visit (ronPerEur = how many RON per 1 EUR). For idempotent import, " +
        "pass sourceContentHash (the SHA-256 you computed over the source document): re-running with the same " +
        "hash returns the existing snapshot instead of duplicating. Returns the captured snapshot including " +
        "its id and rolled-up totals.")]
    public static async Task<ConstructionValuationDto> CaptureConstructionValuation(
        IConstructionValuationAppService service,
        [Description("The valuation catalog id this snapshot is assessed against.")] Guid catalogId,
        [Description("The date of the site visit / assessment (yyyy-MM-dd).")] DateOnly assessedOn,
        [Description("The pinned exchange rate as RON per 1 EUR (curs valutar RON/EUR).")] decimal ronPerEur,
        [Description("The date the pinned rate is as-of (yyyy-MM-dd).")] DateOnly rateAsOf,
        [Description("The per-item completion assessments.")] IReadOnlyList<AssessedItemRow> items,
        [Description("The appraiser's name, if recorded.")] string? appraiser = null,
        [Description("SHA-256 hex digest of the source document, for idempotent import + audit.")] string? sourceContentHash = null,
        [Description("Source document file name, for provenance.")] string? sourceDocumentFileName = null,
        [Description("Source document URL, if stored somewhere.")] string? sourceDocumentUrl = null,
        CancellationToken ct = default)
    {
        var exchangeRate = new ExchangeRateDto(Currency.EUR, Currency.RON, ronPerEur, rateAsOf);

        var command = new CaptureConstructionValuationCommand(
            assessedOn,
            exchangeRate,
            items.Select(i => new AssessedItemInput(i.ValuationCatalogItemId, i.CompletionPercentage)).ToList(),
            appraiser,
            sourceContentHash,
            sourceDocumentFileName,
            sourceDocumentUrl);

        return await service.CaptureAsync(catalogId, command, ct)
               ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");
    }

    [McpServerTool(Name = "get_construction_valuation"), Description(
        "Read a captured snapshot back by its id — its frozen per-item estimated / completed / remaining " +
        "values and per-currency totals. Values reflect the catalog as it was at capture time and never " +
        "recompute.")]
    public static async Task<ConstructionValuationDto> GetConstructionValuation(
        IConstructionValuationAppService service,
        [Description("The snapshot (construction valuation) id.")] Guid valuationId,
        CancellationToken ct = default)
        => await service.GetAsync(valuationId, ct)
           ?? throw new McpException($"No construction valuation exists with id {valuationId}.");

    [McpServerTool(Name = "list_construction_valuations"), Description(
        "List all captured snapshots for a valuation catalog (each a dated site-visit assessment), most useful " +
        "to resolve a snapshot id or check whether a document was already imported. Returns null → the catalog " +
        "was not found.")]
    public static async Task<IReadOnlyList<ConstructionValuationDto>> ListConstructionValuations(
        IConstructionValuationAppService service,
        [Description("The valuation catalog id.")] Guid catalogId,
        CancellationToken ct = default)
        => await service.ListByCatalogAsync(catalogId, ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");

    [McpServerTool(Name = "get_valuation_progress"), Description(
        "Read a catalog's completion-progress series across its frozen snapshots: per-snapshot completed/" +
        "remaining totals and, per catalog item, its completion points over time — the data behind a progress " +
        "chart and mortgage-tranche tracking. Throws if the catalog was not found.")]
    public static async Task<ValuationProgressSeriesDto> GetValuationProgress(
        IValuationProgressQuery query,
        [Description("The valuation catalog id.")] Guid catalogId,
        CancellationToken ct = default)
        => await query.GetSeriesAsync(catalogId, ct)
           ?? throw new McpException($"No valuation catalog exists with id {catalogId}.");
}
