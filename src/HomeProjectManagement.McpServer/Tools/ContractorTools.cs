using System.ComponentModel;
using HomeProjectManagement.Application.Contractors;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The conversational-CRM contractor surface: register a contractor from structured fields, edit
/// their contact / fiscal identifiers, and set master-level notes. Thin wrappers over
/// <see cref="IContractorAppService"/>.
/// </summary>
[McpServerToolType]
public static class ContractorTools
{
    [McpServerTool(Name = "get_contractor"), Description(
        "Read a single contractor by id — name, contact, fiscal identifiers, address, and firm-level notes. " +
        "Use list_contractors to resolve the contractorId first.")]
    public static async Task<ContractorDto> GetContractor(
        IContractorAppService service,
        [Description("The contractor id.")] Guid contractorId,
        CancellationToken ct = default)
        => await service.GetAsync(contractorId, ct)
           ?? throw new McpException($"No contractor exists with id {contractorId}.");

    [McpServerTool(Name = "register_contractor"), Description(
        "Register a new contractor (firm) from structured fields. ALWAYS call list_contractors first to " +
        "avoid duplicates. Returns the created contractor including its contractorId, which you then pass " +
        "to open_bid. Put work-package-specific provenance (\"recommended by Luci for this work package\") " +
        "on the opening bid note instead; use the notes field here only for reusable, firm-level notes.")]
    public static async Task<ContractorDto> RegisterContractor(
        IContractorAppService service,
        [Description("Company name (required).")] string name,
        [Description("Primary contact phone, if known.")] string? phone = null,
        [Description("Primary contact email, if known.")] string? email = null,
        [Description("Primary contact person's name, if known.")] string? contactPerson = null,
        [Description("Romanian fiscal code (CUI), if known.")] string? fiscalCode = null,
        [Description("Registration number (Nr. Reg. Com. / J-number), if known.")] string? registrationNumber = null,
        [Description("Reusable, work-package-independent notes about the firm.")] string? notes = null,
        [Description("Ids of the trades this firm performs (from list_trades). Each must be an existing, active trade.")]
        IReadOnlyList<Guid>? tradeIds = null,
        CancellationToken ct = default)
    {
        var contact = contactPerson is null && email is null && phone is null
            ? null
            : new ContactInfoDto(contactPerson, email, phone);

        var command = new RegisterContractorCommand(name, fiscalCode, registrationNumber, contact, Address: null, notes, tradeIds);
        return await service.RegisterAsync(command, ct);
    }

    [McpServerTool(Name = "update_contractor"), Description(
        "Update a contractor's name, contact details, and fiscal identifiers. Pass the full set of values " +
        "you want the contractor to end up with (omitted optional fields are cleared). Returns the updated contractor.")]
    public static async Task<ContractorDto> UpdateContractor(
        IContractorAppService service,
        [Description("The contractor id.")] Guid contractorId,
        [Description("Company name (required).")] string name,
        [Description("Primary contact phone.")] string? phone = null,
        [Description("Primary contact email.")] string? email = null,
        [Description("Primary contact person's name.")] string? contactPerson = null,
        [Description("Romanian fiscal code (CUI).")] string? fiscalCode = null,
        [Description("Registration number (Nr. Reg. Com.).")] string? registrationNumber = null,
        [Description("Firm-level notes.")] string? notes = null,
        [Description("The full set of trade ids the firm performs (from list_trades); replaces the existing set. " +
            "Omit (null) to leave the existing trades unchanged; pass an empty list to clear them. To add or " +
            "remove a single trade, prefer add_contractor_trade / remove_contractor_trade. Each must be active.")]
        IReadOnlyList<Guid>? tradeIds = null,
        CancellationToken ct = default)
    {
        var contact = contactPerson is null && email is null && phone is null
            ? null
            : new ContactInfoDto(contactPerson, email, phone);

        var command = new UpdateContractorCommand(name, fiscalCode, registrationNumber, contact, Address: null, notes, tradeIds);
        return await service.UpdateAsync(contractorId, command, ct)
               ?? throw new McpException($"No contractor exists with id {contractorId}.");
    }

    [McpServerTool(Name = "add_contractor_trade"), Description(
        "Tag a contractor with one trade it performs, without disturbing its other trades. Resolve the " +
        "tradeId with list_trades first (or create it with define_trade). Idempotent — adding a trade the " +
        "firm already performs is a no-op. Returns the updated contractor.")]
    public static async Task<ContractorDto> AddContractorTrade(
        IContractorAppService service,
        [Description("The contractor id (from list_contractors).")] Guid contractorId,
        [Description("The id of the trade to add (from list_trades). Must be an existing, active trade.")] Guid tradeId,
        CancellationToken ct = default)
        => await service.AddTradeAsync(contractorId, tradeId, ct)
           ?? throw new McpException($"No contractor exists with id {contractorId}.");

    [McpServerTool(Name = "remove_contractor_trade"), Description(
        "Remove one trade tag from a contractor, leaving its other trades in place. Idempotent — removing a " +
        "trade the firm does not perform is a no-op. Returns the updated contractor.")]
    public static async Task<ContractorDto> RemoveContractorTrade(
        IContractorAppService service,
        [Description("The contractor id (from list_contractors).")] Guid contractorId,
        [Description("The id of the trade to remove.")] Guid tradeId,
        CancellationToken ct = default)
        => await service.RemoveTradeAsync(contractorId, tradeId, ct)
           ?? throw new McpException($"No contractor exists with id {contractorId}.");

    [McpServerTool(Name = "annotate_contractor"), Description(
        "Set the contractor's master-level notes (e.g. provenance that is independent of any one work " +
        "package), leaving the rest of the contractor unchanged. Returns the updated contractor.")]
    public static async Task<ContractorDto> AnnotateContractor(
        IContractorAppService service,
        [Description("The contractor id.")] Guid contractorId,
        [Description("The notes to set on the contractor.")] string notes,
        CancellationToken ct = default)
    {
        var existing = await service.GetAsync(contractorId, ct)
                       ?? throw new McpException($"No contractor exists with id {contractorId}.");

        var command = new UpdateContractorCommand(
            existing.Name, existing.FiscalCode, existing.RegistrationNumber,
            existing.Contact, existing.Address, notes, existing.TradeIds);

        return await service.UpdateAsync(contractorId, command, ct)
               ?? throw new McpException($"No contractor exists with id {contractorId}.");
    }
}
