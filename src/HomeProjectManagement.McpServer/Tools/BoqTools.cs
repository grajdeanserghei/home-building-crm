using System.ComponentModel;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.BillsOfQuantities;
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
        "Create the draft Bill of Quantities for a bid (open_bid first — a BoQ belongs to a bid, through " +
        "which its work package and contractor are reached). There is at most one BoQ per bid. Supply the " +
        "pricing currency (RON or EUR); all line prices must be in it. For idempotent ingestion, pass " +
        "sourceContentHash (the SHA-256 you computed over the source PDF): re-running with the same hash " +
        "returns the existing BoQ instead of duplicating. If a BoQ already exists for the bid from a " +
        "different document, this is rejected — use replace_boq_contents to supersede it with the revised " +
        "deviz. Set budgetScopeKind from what the deviz is priced against: EntireBuilding (the default) " +
        "or PerApartment when it is the price for a single apartment (its build-wide cost is then that " +
        "total times the project's apartment count). Returns the draft BoQ including its boqId.")]
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
        [Description("What the deviz is priced against: EntireBuilding (default) or PerApartment (the price for one apartment).")] BudgetScopeKind budgetScopeKind = BudgetScopeKind.EntireBuilding,
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
            sourceContentHash, sourceDocumentFileName, sourceDocumentUrl, budgetScopeKind);

        return await service.DraftAsync(bidId, command, ct)
               ?? throw new McpException($"No bid exists with id {bidId}.");
    }

    [McpServerTool(Name = "replace_boq_contents"), Description(
        "Replace an existing BoQ's contents in place when the contractor sends a revised deviz — clears its " +
        "sections, subsections, and line items and re-points the header + provenance (reference, rate, dates, " +
        "source document/hash) to the new document, ready to re-ingest with add_boq_sections / " +
        "add_boq_line_items. Use this instead of create_boq when get_bid_boq shows the bid already has a BoQ. " +
        "Only works while the BoQ is still editable (Draft/Submitted). The pricing currency is fixed and " +
        "cannot change. Returns the now-empty BoQ.")]
    public static async Task<BillOfQuantitiesDto> ReplaceBoqContents(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id (from get_bid_boq).")] Guid boqId,
        [Description("The contractor's own deviz number/label on the revised document, if any.")] string? reference = null,
        [Description("SHA-256 hex digest of the revised source PDF, for idempotency + audit.")] string? sourceContentHash = null,
        [Description("Revised source document file name, for provenance.")] string? sourceDocumentFileName = null,
        [Description("Revised source document URL, if stored somewhere.")] string? sourceDocumentUrl = null,
        [Description("Pinned exchange rate base currency (e.g. EUR), if pinning a EUR↔RON rate.")] Currency? exchangeRateBaseCurrency = null,
        [Description("Pinned exchange rate quote currency (e.g. RON).")] Currency? exchangeRateQuoteCurrency = null,
        [Description("Pinned exchange rate value (units of quote per base).")] decimal? exchangeRate = null,
        [Description("The date the pinned rate is as-of (yyyy-MM-dd).")] DateOnly? exchangeRateAsOf = null,
        [Description("When the revised quote was received (absolute ISO timestamp).")] DateTimeOffset? submittedOn = null,
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

        var command = new ReplaceBoqContentsCommand(
            reference, rate, submittedOn, validUntil,
            sourceContentHash, sourceDocumentFileName, sourceDocumentUrl);

        return await service.ReplaceContentsAsync(boqId, command, ct)
               ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
    }

    [McpServerTool(Name = "update_boq"), Description(
        "Edit a BoQ's header details in place: what the quote is priced against (budgetScopeKind / " +
        "'Tarifat pentru'), the contractor's deviz reference, the pinned EUR↔RON exchange rate, and the " +
        "submitted/valid-until dates. Every argument is optional — omit a field to leave it unchanged " +
        "(the current value is preserved). The pricing currency is fixed at draft time and cannot change " +
        "here. Editable only while the BoQ is Draft or Submitted; a closed BoQ (Accepted/Rejected/" +
        "Withdrawn) is rejected. Set budgetScopeKind to EntireBuilding or PerApartment to change " +
        "'Tarifat pentru'. To pin a new rate, supply all four exchangeRate* fields together. Returns the " +
        "updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> UpdateBoq(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id (from get_bid_boq / get_boq).")] Guid boqId,
        [Description("The contractor's own deviz number/label. Omit to keep the current reference.")] string? reference = null,
        [Description("What the deviz is priced against: EntireBuilding or PerApartment. Omit to keep the current scope.")] BudgetScopeKind? budgetScopeKind = null,
        [Description("When the quote was received (absolute ISO timestamp). Omit to keep the current value.")] DateTimeOffset? submittedOn = null,
        [Description("Offer expiry (absolute ISO timestamp). Omit to keep the current value.")] DateTimeOffset? validUntil = null,
        [Description("Pinned exchange rate base currency (e.g. EUR). Supply all four exchangeRate* fields to pin a rate; omit all to keep the current rate.")] Currency? exchangeRateBaseCurrency = null,
        [Description("Pinned exchange rate quote currency (e.g. RON).")] Currency? exchangeRateQuoteCurrency = null,
        [Description("Pinned exchange rate value (units of quote per base).")] decimal? exchangeRate = null,
        [Description("The date the pinned rate is as-of (yyyy-MM-dd).")] DateOnly? exchangeRateAsOf = null,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        ExchangeRateDto? rate;
        if (exchangeRateBaseCurrency is { } baseCurrency
            && exchangeRateQuoteCurrency is { } quoteCurrency
            && exchangeRate is { } value
            && exchangeRateAsOf is { } asOf)
        {
            rate = new ExchangeRateDto(baseCurrency, quoteCurrency, value, asOf);
        }
        else
        {
            rate = boq.ExchangeRate;
        }

        var command = new UpdateBillOfQuantitiesCommand(
            reference ?? boq.Reference,
            rate,
            submittedOn ?? boq.SubmittedOn,
            validUntil ?? boq.ValidUntil,
            budgetScopeKind ?? boq.BudgetScopeKind);

        return await service.UpdateAsync(boqId, command, ct)
               ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
    }

    [McpServerTool(Name = "set_boq_budget_scope"), Description(
        "Change what a BoQ is priced against ('Tarifat pentru'): EntireBuilding (the whole building) or " +
        "PerApartment (the price for a single apartment, whose build-wide cost is that total times the " +
        "project's apartment count). Leaves every other header field (reference, exchange rate, dates) " +
        "untouched. Editable only while the BoQ is Draft or Submitted; a closed BoQ is rejected. Returns " +
        "the updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> SetBoqBudgetScope(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id (from get_bid_boq / get_boq).")] Guid boqId,
        [Description("What the deviz is priced against: EntireBuilding or PerApartment.")] BudgetScopeKind budgetScopeKind,
        CancellationToken ct = default)
    {
        var boq = await service.GetAsync(boqId, ct)
                  ?? throw new McpException($"No bill of quantities exists with id {boqId}.");

        var command = new UpdateBillOfQuantitiesCommand(
            boq.Reference,
            boq.ExchangeRate,
            boq.SubmittedOn,
            boq.ValidUntil,
            budgetScopeKind);

        return await service.UpdateAsync(boqId, command, ct)
               ?? throw new McpException($"No bill of quantities exists with id {boqId}.");
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

    [McpServerTool(Name = "revise_boq_section"), Description(
        "Rename, reorder, or re-describe an existing section of a BoQ — edits the section itself, not its " +
        "line items. Provide all of name, sequence, and description (omit description to clear it). Only " +
        "works while the BoQ is still editable (Draft/Submitted). Returns the updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> ReviseBoqSection(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id to revise.")] Guid sectionId,
        [Description("The corrected section name.")] string name,
        [Description("Order of the section within the BoQ.")] int sequence,
        [Description("Section description (omit to clear it).")] string? description = null,
        CancellationToken ct = default)
    {
        return await service.UpdateSectionAsync(
                   boqId, sectionId, new SectionCommand(name, sequence, description), ct)
               ?? throw new McpException(
                   $"BoQ {boqId} or section {sectionId} was not found.");
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

    [McpServerTool(Name = "revise_boq_subsection"), Description(
        "Rename, reorder, or re-describe an existing subsection of a BoQ — edits the subsection itself, not " +
        "its line items. Provide all of name, sequence, and description (omit description to clear it). Only " +
        "works while the BoQ is still editable (Draft/Submitted). Returns the updated BoQ.")]
    public static async Task<BillOfQuantitiesDto> ReviseBoqSubsection(
        IBillOfQuantitiesAppService service,
        [Description("The BoQ id.")] Guid boqId,
        [Description("The section id.")] Guid sectionId,
        [Description("The subsection id to revise.")] Guid subsectionId,
        [Description("The corrected subsection name.")] string name,
        [Description("Order of the subsection within the section.")] int sequence,
        [Description("Subsection description (omit to clear it).")] string? description = null,
        CancellationToken ct = default)
    {
        return await service.UpdateSubsectionAsync(
                   boqId, sectionId, subsectionId, new SubsectionCommand(name, sequence, description), ct)
               ?? throw new McpException(
                   $"BoQ {boqId}, section {sectionId}, or subsection {subsectionId} was not found.");
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

    [McpServerTool(Name = "get_bid_boq"), Description(
        "Get the single Bill of Quantities for a bid (a bid has at most one BoQ), or null if none has been " +
        "created yet. Use this to discover whether a bid already has a BoQ — and its boqId — before creating " +
        "or replacing one. Resolve the bidId via list_bids / get_bid first.")]
    public static async Task<BillOfQuantitiesDto?> GetBidBoq(
        IBillOfQuantitiesAppService service,
        [Description("The owning bid id (from list_bids / get_bid).")] Guid bidId,
        CancellationToken ct = default)
        => await service.GetByBidAsync(bidId, ct);

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
