using System.ComponentModel;
using HomeProjectManagement.Application.CostScenarios;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The cost-simulator surface: build and read saved "what-if" cost scenarios — for each work package
/// a chosen bid (its bill of quantities) supplies the cost, and the combined total is computed on the
/// fly (per currency, with an approximate EUR-equivalent). Use <c>get_cost_scenario_candidates</c> to
/// discover the priced bids per work package, then <c>include_bid_in_scenario</c> to pick one each, and
/// <c>get_cost_scenario</c> to see the total. Thin wrappers over <see cref="ICostScenarioAppService"/>
/// (mutations) and <see cref="ICostScenarioQuery"/> (computed reads).
/// </summary>
[McpServerToolType]
public static class CostScenarioTools
{
    [McpServerTool(Name = "list_cost_scenarios"), Description(
        "List a project's saved cost scenarios as lightweight summaries (id, name, work-package count). " +
        "Use it to resolve a scenarioId before reading or editing one.")]
    public static async Task<IReadOnlyList<CostScenarioSummaryDto>> ListCostScenarios(
        ICostScenarioAppService service,
        [Description("The project whose scenarios to list.")] Guid projectId,
        CancellationToken ct = default)
        => await service.ListByProjectAsync(projectId, ct);

    [McpServerTool(Name = "create_cost_scenario"), Description(
        "Create a new, empty cost scenario in a project. Add bids to it afterwards with " +
        "include_bid_in_scenario. Returns the created scenario including its id.")]
    public static async Task<CostScenarioSummaryDto> CreateCostScenario(
        ICostScenarioAppService service,
        [Description("The project the scenario belongs to.")] Guid projectId,
        [Description("A short name for the scenario, e.g. \"Cheapest per package\".")] string name,
        [Description("Optional free-text description.")] string? description = null,
        CancellationToken ct = default)
        => await service.CreateAsync(projectId, new CreateCostScenarioCommand(name, description), ct)
           ?? throw new McpException($"No project exists with id {projectId}.");

    [McpServerTool(Name = "get_cost_scenario"), Description(
        "Read a scenario's computed cost picture: a line per included bill of quantities plus per-currency " +
        "net/gross totals and an approximate EUR-equivalent. This is the \"see the total cost\" tool. " +
        "Figures are scaled to the whole build (a per-apartment quote is multiplied by the apartment count).")]
    public static async Task<CostScenarioResultDto> GetCostScenario(
        ICostScenarioQuery query,
        [Description("The scenario id.")] Guid scenarioId,
        CancellationToken ct = default)
        => await query.GetAsync(scenarioId, ct)
           ?? throw new McpException($"No cost scenario exists with id {scenarioId}.");

    [McpServerTool(Name = "get_cost_scenario_candidates"), Description(
        "List, per work package in a project, the priced bids available to choose from (contractor name plus " +
        "effective net/gross totals). Use it to pick which bidId to include for each work package.")]
    public static async Task<IReadOnlyList<ScenarioCandidateWorkPackageDto>> GetCostScenarioCandidates(
        ICostScenarioQuery query,
        [Description("The project whose candidate bids to list.")] Guid projectId,
        CancellationToken ct = default)
        => await query.GetCandidatesAsync(projectId, ct)
           ?? throw new McpException($"No project exists with id {projectId}.");

    [McpServerTool(Name = "include_bid_in_scenario"), Description(
        "Choose a bid for a work package within a scenario (one bid per work package — replaces any existing " +
        "choice for that work package). The bid must belong to a work package in the scenario's project. " +
        "Returns the scenario's recomputed cost picture.")]
    public static async Task<CostScenarioResultDto> IncludeBidInScenario(
        ICostScenarioAppService service,
        ICostScenarioQuery query,
        [Description("The scenario id.")] Guid scenarioId,
        [Description("The work package the choice is for.")] Guid workPackageId,
        [Description("The chosen bid whose BoQ supplies the cost.")] Guid bidId,
        CancellationToken ct = default)
    {
        var ok = await service.IncludeBidAsync(scenarioId, new IncludeBidCommand(workPackageId, bidId), ct);
        if (!ok)
        {
            throw new McpException($"No cost scenario exists with id {scenarioId}.");
        }

        return await query.GetAsync(scenarioId, ct)
               ?? throw new McpException($"No cost scenario exists with id {scenarioId}.");
    }

    [McpServerTool(Name = "remove_work_package_from_scenario"), Description(
        "Exclude a work package from a scenario (drop its chosen bid). Returns the scenario's recomputed " +
        "cost picture.")]
    public static async Task<CostScenarioResultDto> RemoveWorkPackageFromScenario(
        ICostScenarioAppService service,
        ICostScenarioQuery query,
        [Description("The scenario id.")] Guid scenarioId,
        [Description("The work package to exclude.")] Guid workPackageId,
        CancellationToken ct = default)
    {
        var ok = await service.RemoveWorkPackageAsync(scenarioId, workPackageId, ct);
        if (!ok)
        {
            throw new McpException($"No cost scenario exists with id {scenarioId}.");
        }

        return await query.GetAsync(scenarioId, ct)
               ?? throw new McpException($"No cost scenario exists with id {scenarioId}.");
    }

    [McpServerTool(Name = "delete_cost_scenario"), Description(
        "Delete a cost scenario. Returns a confirmation message.")]
    public static async Task<string> DeleteCostScenario(
        ICostScenarioAppService service,
        [Description("The scenario id.")] Guid scenarioId,
        CancellationToken ct = default)
        => await service.DeleteAsync(scenarioId, ct)
            ? $"Deleted cost scenario {scenarioId}."
            : throw new McpException($"No cost scenario exists with id {scenarioId}.");
}
