using System.ComponentModel;
using HomeProjectManagement.Application.WorkPackages;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The work-package authoring surface: define a new procurement unit (e.g. La Roșu, Tâmplărie)
/// under a project and edit its descriptive fields and schedule. Thin wrappers over
/// <see cref="IWorkPackageAppService"/>. Status transitions, scope items, and awarding are
/// deliberately out of scope here — they carry their own invariants and belong on dedicated tools.
/// </summary>
[McpServerToolType]
public static class WorkPackageTools
{
    [McpServerTool(Name = "get_work_package"), Description(
        "Read a single work package by id — its name, description, status, order, planned schedule, " +
        "awarded contract (if any), and scope items. Use list_work_packages to resolve the workPackageId first.")]
    public static async Task<WorkPackageDto> GetWorkPackage(
        IWorkPackageAppService service,
        [Description("The work package id.")] Guid workPackageId,
        CancellationToken ct = default)
        => await service.GetAsync(workPackageId, ct)
           ?? throw new McpException($"No work package exists with id {workPackageId}.");

    [McpServerTool(Name = "define_work_package"), Description(
        "Define (create) a new work package — a unit of work procured as one, e.g. La Roșu or Tâmplărie — " +
        "under a project. Resolve the projectId with list_projects first. A freshly defined package starts " +
        "in status Defined. Returns the created work package including its workPackageId, which you then pass " +
        "to open_bid.")]
    public static async Task<WorkPackageDto> DefineWorkPackage(
        IWorkPackageAppService service,
        [Description("The owning project id (from list_projects).")] Guid projectId,
        [Description("Work package name (required, e.g. La Roșu).")] string name,
        [Description("Free-text scope notes, if any.")] string? description = null,
        [Description("Display/intended order within the project (1-based). Defaults to 1.")] int sequence = 1,
        [Description("Optional planned start (absolute ISO timestamp).")] DateTimeOffset? plannedStartDate = null,
        [Description("Optional planned end (absolute ISO timestamp). Must not precede the start.")] DateTimeOffset? plannedEndDate = null,
        [Description("Ids of the trades this package requires (from list_trades). Each must be an existing, active trade. May be empty.")]
        IReadOnlyList<Guid>? requiredTradeIds = null,
        CancellationToken ct = default)
    {
        var command = new DefineWorkPackageCommand(name, description, sequence, plannedStartDate, plannedEndDate, requiredTradeIds);
        return await service.DefineAsync(projectId, command, ct)
               ?? throw new McpException($"No project exists with id {projectId}.");
    }

    [McpServerTool(Name = "update_work_package"), Description(
        "Update a work package's name, description, order, and planned schedule. Pass the full set of values " +
        "you want the package to end up with (omitted optional fields are cleared). Status is not changed here. " +
        "Returns the updated work package.")]
    public static async Task<WorkPackageDto> UpdateWorkPackage(
        IWorkPackageAppService service,
        [Description("The work package id.")] Guid workPackageId,
        [Description("Work package name (required).")] string name,
        [Description("Free-text scope notes.")] string? description = null,
        [Description("Display/intended order within the project (1-based). Defaults to 1.")] int sequence = 1,
        [Description("Planned start (absolute ISO timestamp).")] DateTimeOffset? plannedStartDate = null,
        [Description("Planned end (absolute ISO timestamp). Must not precede the start.")] DateTimeOffset? plannedEndDate = null,
        [Description("The full set of trade ids this package requires (from list_trades); replaces the existing set. " +
            "Omit (null) to leave the existing required trades unchanged; pass an empty list to clear them. To add " +
            "or remove a single trade, prefer add_work_package_trade / remove_work_package_trade. Each must be active.")]
        IReadOnlyList<Guid>? requiredTradeIds = null,
        CancellationToken ct = default)
    {
        var command = new UpdateWorkPackageCommand(name, description, sequence, plannedStartDate, plannedEndDate, requiredTradeIds);
        return await service.UpdateAsync(workPackageId, command, ct)
               ?? throw new McpException($"No work package exists with id {workPackageId}.");
    }

    [McpServerTool(Name = "add_work_package_trade"), Description(
        "Require one trade for a work package, without disturbing its other required trades. Resolve the " +
        "tradeId with list_trades first (or create it with define_trade). Idempotent — adding a trade the " +
        "package already requires is a no-op. Returns the updated work package.")]
    public static async Task<WorkPackageDto> AddWorkPackageTrade(
        IWorkPackageAppService service,
        [Description("The work package id (from list_work_packages).")] Guid workPackageId,
        [Description("The id of the trade to require (from list_trades). Must be an existing, active trade.")] Guid tradeId,
        CancellationToken ct = default)
        => await service.AddRequiredTradeAsync(workPackageId, tradeId, ct)
           ?? throw new McpException($"No work package exists with id {workPackageId}.");

    [McpServerTool(Name = "remove_work_package_trade"), Description(
        "Drop one required trade from a work package, leaving its other required trades in place. Idempotent — " +
        "removing a trade the package does not require is a no-op. Returns the updated work package.")]
    public static async Task<WorkPackageDto> RemoveWorkPackageTrade(
        IWorkPackageAppService service,
        [Description("The work package id (from list_work_packages).")] Guid workPackageId,
        [Description("The id of the trade to drop.")] Guid tradeId,
        CancellationToken ct = default)
        => await service.RemoveRequiredTradeAsync(workPackageId, tradeId, ct)
           ?? throw new McpException($"No work package exists with id {workPackageId}.");
}
