using System.ComponentModel;
using HomeProjectManagement.Application.Trades;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The trade-vocabulary surface: define a new construction trade and edit an existing one. Thin
/// wrappers over <see cref="ITradeAppService"/>. Trade is a controlled, project-independent
/// vocabulary — ALWAYS call list_trades first to reuse an existing trade rather than creating a
/// near-duplicate (e.g. "Electrice" vs "Instalații Electrice"). Trades are retired, never deleted.
/// </summary>
[McpServerToolType]
public static class TradeTools
{
    [McpServerTool(Name = "define_trade"), Description(
        "Define a new trade in the controlled vocabulary (e.g. Zidărie, Instalații Electrice). ALWAYS call " +
        "list_trades first to avoid creating a duplicate. The name must be unique. Returns the created trade " +
        "including its tradeId, which you then pass when tagging a contractor or work package.")]
    public static async Task<TradeDto> DefineTrade(
        ITradeAppService service,
        [Description("Canonical trade name (required, unique; displayed in Romanian, e.g. Tâmplărie).")] string name,
        [Description("Optional short code (e.g. ELE).")] string? code = null,
        CancellationToken ct = default)
    {
        var result = await service.DefineAsync(new DefineTradeCommand(name, code), ct);
        return result.Created
               ?? throw new McpException($"A trade named '{result.ConflictName}' already exists.");
    }

    [McpServerTool(Name = "update_trade"), Description(
        "Update a trade's name and short code. Pass the full set of values you want it to end up with. " +
        "Returns the updated trade.")]
    public static async Task<TradeDto> UpdateTrade(
        ITradeAppService service,
        [Description("The trade id (from list_trades).")] Guid tradeId,
        [Description("Canonical trade name (required, unique).")] string name,
        [Description("Optional short code.")] string? code = null,
        CancellationToken ct = default)
        => await service.UpdateAsync(tradeId, new UpdateTradeCommand(name, code), ct)
           ?? throw new McpException($"No trade exists with id {tradeId}.");

    [McpServerTool(Name = "set_trade_active"), Description(
        "Retire (deactivate) or reinstate (activate) a trade. Retired trades stay valid for existing " +
        "references but are not offered for new ones. Trades are never deleted.")]
    public static async Task<string> SetTradeActive(
        ITradeAppService service,
        [Description("The trade id (from list_trades).")] Guid tradeId,
        [Description("True to reinstate, false to retire.")] bool isActive,
        CancellationToken ct = default)
        => await service.SetActiveAsync(tradeId, isActive, ct)
            ? $"Trade {tradeId} is now {(isActive ? "active" : "retired")}."
            : throw new McpException($"No trade exists with id {tradeId}.");
}
