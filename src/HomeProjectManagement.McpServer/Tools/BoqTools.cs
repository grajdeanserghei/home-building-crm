using System.ComponentModel;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The document-ingestion BoQ surface. The agent reads the PDF <c>deviz</c> and emits structured
/// data; these tools validate, normalise units, compute totals, and persist. The server never parses
/// PDFs. Thin wrappers over <see cref="IBillOfQuantitiesAppService"/>.
/// </summary>
[McpServerToolType]
public static class BoqTools
{
    /// <summary>One raw BoQ line as the agent extracted it; the unit is a free-text token to normalise.</summary>
    public sealed record BoqLineRow(
        [property: Description("The work/material item description.")] string Description,
        [property: Description("Free-text unit token as it appears in the deviz (e.g. mc, m³, buc).")] string Unit,
        [property: Description("Quantity.")] decimal Quantity,
        [property: Description("Net (VAT-exclusive) unit price, in the BoQ's pricing currency.")] decimal UnitPrice,
        [property: Description("VAT rate percentage (e.g. 21). Defaults to 21 when omitted.")] decimal? VatRatePercentage = null,
        [property: Description("Optional line notes.")] string? Notes = null);

    /// <summary>One section to add to a BoQ.</summary>
    public sealed record BoqSectionRow(
        [property: Description("Section name (e.g. Foundation, Roof).")] string Name,
        [property: Description("Optional explicit order; assigned automatically when omitted.")] int? Sequence = null,
        [property: Description("Optional section description.")] string? Description = null);

    [McpServerTool(Name = "create_boq"), Description(
        "Create a draft Bill of Quantities under a bid (open_bid first — a BoQ belongs to a bid, through " +
        "which its work package and contractor are reached). Supply the pricing currency (RON or EUR); all " +
        "line prices must be in it. For idempotent ingestion, pass sourceContentHash (the SHA-256 you " +
        "computed over the source PDF): re-running with the same hash returns the existing BoQ instead of " +
        "duplicating. Returns the draft BoQ including its boqId.")]
    public static async Task<BillOfQuantitiesDto> CreateBoq(
        IBillOfQuantitiesAppService service,
        [Description("The owning bid id (from open_bid).")] Guid bidId,
        [Description("Pricing currency: RON or EUR.")] Currency pricingCurrency,
        [Description("The contractor's own deviz number/label, if any.")] string? reference = null,
        [Description("SHA-256 hex digest of the source PDF, for idempotent ingestion + audit.")] string? sourceContentHash = null,
        [Description("Source document file name, for provenance.")] string? sourceDocumentFileName = null,
        [Description("Source document URL, if stored somewhere.")] string? sourceDocumentUrl = null,
        [Description("Pinned exchange rate base currency (e.g. EUR), if pinning a EUR↔RON rate.")] Currency? exchangeRateBaseCurrency = null,
        [Description("Pinned exchange rate quote currency (e.g. RON).")] Currency? exchangeRateQuoteCurrency = null,
        [Description("Pinned exchange rate value (units of quote per base).")] decimal? exchangeRate = null,
        [Description("The date the pinned rate is as-of (yyyy-MM-dd).")] DateOnly? exchangeRateAsOf = null,
        [Description("When the quote was received (absolute ISO timestamp).")] DateTimeOffset? submittedOn = null,
        [Description("Offer expiry (absolute ISO timestamp).")] DateTimeOffset? validUntil = null,
        CancellationToken ct = default)
    {
        ExchangeRateDto? rate = null;
        if (exchangeRateBaseCurrency is { } baseCurrency
            && exchangeRateQuoteCurrency is { } quoteCurrency
            && exchangeRate is { } value
            && exchangeRateAsOf is { } asOf)
        {
            rate = new ExchangeRateDto(baseCurrency, quoteCurrency, value, asOf);
        }

        var command = new DraftBillOfQuantitiesCommand(
            pricingCurrency, reference, rate, submittedOn, validUntil,
            sourceContentHash, sourceDocumentFileName, sourceDocumentUrl);

        return await service.DraftAsync(bidId, command, ct)
               ?? throw new McpException($"No bid exists with id {bidId}.");
    }

    [McpServerTool(Name = "add_boq_sections"), Description(
        "Add one or more sections to a draft BoQ (the deviz's chapters). Sequence is assigned automatically " +
        "when omitted. Returns the updated BoQ; read each section's id from it before adding line items.")]
    public static async Task<BillOfQuantitiesDto> AddBoqSections(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The sections to add.")] IReadOnlyList<BoqSectionRow> sections,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var nextSequence = boq.Sections.Count == 0 ? 1 : boq.Sections.Max(s => s.Sequence) + 1;

        BillOfQuantitiesDto? updated = boq;
        foreach (var section in sections)
        {
            updated = await service.AddSectionAsync(
                boqId,
                new SectionCommand(section.Name, section.Sequence ?? nextSequence++, section.Description),
                ct);
        }

        return updated ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
    }

    [McpServerTool(Name = "add_boq_line_items"), Description(
        "Bulk-add line items to a section of a draft BoQ — a whole deviz section in one call. Each line's " +
        "free-text unit token is normalised onto an active canonical unit of measure; line totals are " +
        "computed server-side. Lines whose unit can't be matched are returned in 'unresolved' (flagged with " +
        "the offending token) while the resolvable lines still persist — a single bad unit does not fail the " +
        "batch. Unit prices are taken to be in the BoQ's pricing currency. Check 'unresolved' and fix those " +
        "units (ask an admin to add the missing unit) before submitting.")]
    public static async Task<AddBoqLineItemsResult> AddBoqLineItems(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id (from add_boq_sections / get_boq).")] Guid sectionId,
        [Description("The line items to add.")] IReadOnlyList<BoqLineRow> items,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var inputs = items
            .Select(row => new BoqLineItemInput(
                row.Description,
                row.Unit,
                row.Quantity,
                new MoneyDto(row.UnitPrice, boq.PricingCurrency),
                row.VatRatePercentage,
                row.Notes))
            .ToList();

        return await service.AddLineItemsAsync(boqId, sectionId, inputs, ct)
               ?? throw new McpException(
                   $"BoQ {boqId} or section {sectionId} was not found.");
    }

    [McpServerTool(Name = "revise_boq_line_item"), Description(
        "Correct a single line on a draft BoQ. Provide the resolved unitOfMeasureId (from " +
        "list_units_of_measure). The unit price is taken to be in the BoQ's pricing currency. Returns the " +
        "updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> ReviseBoqLineItem(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The line item id to revise.")] Guid lineItemId,
        [Description("The corrected description.")] string description,
        [Description("The corrected quantity.")] decimal quantity,
        [Description("The resolved canonical unit id (from list_units_of_measure).")] Guid unitOfMeasureId,
        [Description("The corrected net unit price, in the BoQ's pricing currency.")] decimal unitPrice,
        [Description("Order of the line within the section.")] int sequence,
        [Description("VAT rate percentage (defaults to 21 when omitted).")] decimal? vatRatePercentage = null,
        [Description("Optional line notes.")] string? notes = null,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var command = new LineItemCommand(
            description, quantity, unitOfMeasureId,
            new MoneyDto(unitPrice, boq.PricingCurrency), vatRatePercentage, sequence, notes);

        return await service.ReviseLineItemAsync(boqId, sectionId, lineItemId, command, ct)
               ?? throw new McpException(
                   $"BoQ {boqId}, section {sectionId}, or line item {lineItemId} was not found.");
    }

    [McpServerTool(Name = "remove_boq_line_item"), Description(
        "Remove a single line from a draft BoQ — use this to drop a line added in error or one that doesn't " +
        "belong in the deviz. Only works while the BoQ is a draft (line edits lock on submit). Returns the " +
        "updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> RemoveBoqLineItem(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The line item id to remove.")] Guid lineItemId,
        CancellationToken ct = default)
    {
        if (!await service.RemoveLineItemAsync(boqId, sectionId, lineItemId, ct))
        {
            throw new McpException(
                $"BoQ {boqId}, section {sectionId}, or line item {lineItemId} was not found.");
        }

        return await service.GetAsync(boqId, ct)
               ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
    }

    [McpServerTool(Name = "add_boq_subsections"), Description(
        "Add one or more subsections to a section of a draft BoQ — a fixed second level of grouping (e.g. " +
        "Excavation, Reinforcement within Foundation). A section may hold line items directly and/or grouped " +
        "in subsections. Sequence is assigned automatically when omitted. Returns the updated BoQ; read each " +
        "subsection's id from it before adding its line items.")]
    public static async Task<BillOfQuantitiesDto> AddBoqSubsections(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id (from add_boq_sections / get_boq).")] Guid sectionId,
        [Description("The subsections to add.")] IReadOnlyList<BoqSectionRow> subsections,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var section = boq.Sections.FirstOrDefault(s => s.Id == sectionId)
                      ?? throw new McpException($"BoQ {boqId} has no section {sectionId}.");

        var nextSequence = section.Subsections.Count == 0 ? 1 : section.Subsections.Max(s => s.Sequence) + 1;

        BillOfQuantitiesDto? updated = boq;
        foreach (var subsection in subsections)
        {
            updated = await service.AddSubsectionAsync(
                boqId,
                sectionId,
                new SubsectionCommand(subsection.Name, subsection.Sequence ?? nextSequence++, subsection.Description),
                ct);
        }

        return updated ?? throw new McpException($"BoQ {boqId} or section {sectionId} was not found.");
    }

    [McpServerTool(Name = "add_boq_subsection_line_items"), Description(
        "Bulk-add line items to a subsection of a draft BoQ — the subsection counterpart of " +
        "add_boq_line_items. Each line's free-text unit token is normalised onto an active canonical unit of " +
        "measure; line totals are computed server-side. Lines whose unit can't be matched are returned in " +
        "'unresolved' (flagged with the offending token) while the resolvable lines still persist — a single " +
        "bad unit does not fail the batch. Unit prices are taken to be in the BoQ's pricing currency.")]
    public static async Task<AddBoqLineItemsResult> AddBoqSubsectionLineItems(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The subsection id (from add_boq_subsections / get_boq).")] Guid subsectionId,
        [Description("The line items to add.")] IReadOnlyList<BoqLineRow> items,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var inputs = items
            .Select(row => new BoqLineItemInput(
                row.Description,
                row.Unit,
                row.Quantity,
                new MoneyDto(row.UnitPrice, boq.PricingCurrency),
                row.VatRatePercentage,
                row.Notes))
            .ToList();

        return await service.AddSubsectionLineItemsAsync(boqId, sectionId, subsectionId, inputs, ct)
               ?? throw new McpException(
                   $"BoQ {boqId}, section {sectionId}, or subsection {subsectionId} was not found.");
    }

    [McpServerTool(Name = "revise_boq_subsection_line_item"), Description(
        "Correct a single line inside a subsection of a draft BoQ. Provide the resolved unitOfMeasureId (from " +
        "list_units_of_measure). The unit price is taken to be in the BoQ's pricing currency. Returns the updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> ReviseBoqSubsectionLineItem(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The subsection id.")] Guid subsectionId,
        [Description("The line item id to revise.")] Guid lineItemId,
        [Description("The corrected description.")] string description,
        [Description("The corrected quantity.")] decimal quantity,
        [Description("The resolved canonical unit id (from list_units_of_measure).")] Guid unitOfMeasureId,
        [Description("The corrected net unit price, in the BoQ's pricing currency.")] decimal unitPrice,
        [Description("Order of the line within the subsection.")] int sequence,
        [Description("VAT rate percentage (defaults to 21 when omitted).")] decimal? vatRatePercentage = null,
        [Description("Optional line notes.")] string? notes = null,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var command = new LineItemCommand(
            description, quantity, unitOfMeasureId,
            new MoneyDto(unitPrice, boq.PricingCurrency), vatRatePercentage, sequence, notes);

        return await service.ReviseSubsectionLineItemAsync(boqId, sectionId, subsectionId, lineItemId, command, ct)
               ?? throw new McpException(
                   $"BoQ {boqId}, section {sectionId}, subsection {subsectionId}, or line item {lineItemId} was not found.");
    }

    [McpServerTool(Name = "remove_boq_subsection_line_item"), Description(
        "Remove a single line from a subsection of a draft BoQ — use this to drop a line added in error. Only " +
        "works while the BoQ is a draft (line edits lock on submit). Returns the updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> RemoveBoqSubsectionLineItem(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The subsection id.")] Guid subsectionId,
        [Description("The line item id to remove.")] Guid lineItemId,
        CancellationToken ct = default)
    {
        if (!await service.RemoveSubsectionLineItemAsync(boqId, sectionId, subsectionId, lineItemId, ct))
        {
            throw new McpException(
                $"BoQ {boqId}, section {sectionId}, subsection {subsectionId}, or line item {lineItemId} was not found.");
        }

        return await service.GetAsync(boqId, ct)
               ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
    }

    [McpServerTool(Name = "list_boqs"), Description(
        "List the Bills of Quantities submitted within a bid (oldest version first; a bid may hold several " +
        "revisions). Use this to discover which BoQs exist for a bid — and their boqIds — before reading one " +
        "with get_boq. Resolve the bidId via list_bids / get_bid first.")]
    public static async Task<IReadOnlyList<BillOfQuantitiesDto>> ListBoqs(
        IBillOfQuantitiesAppService service,
        [Description("The owning bid id (from list_bids / get_bid).")] Guid bidId,
        CancellationToken ct = default)
        => await service.ListByBidAsync(bidId, ct);

    [McpServerTool(Name = "get_boq"), Description(
        "Read a BoQ back — its sections, line items, and derived totals — to verify an ingestion before submitting.")]
    public static async Task<BillOfQuantitiesDto> GetBoq(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        CancellationToken ct = default)
        => await service.GetAsync(boqId, ct)
           ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

    [McpServerTool(Name = "submit_boq"), Description(
        "Submit a draft BoQ (Draft → Submitted, locking line edits) and record receipt on the owning bid " +
        "(moving it to BoqReceived). Call this once you've verified the BoQ with get_boq. Returns the submitted BoQ.")]
    public static async Task<BillOfQuantitiesDto> SubmitBoq(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        CancellationToken ct = default)
        => await service.SubmitAsync(boqId, ct)
           ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
}
