using System.ComponentModel;
using HomeProjectManagement.Application.Contractors;
using HomeProjectManagement.Application.Projects;
using HomeProjectManagement.Application.UnitsOfMeasure;
using HomeProjectManagement.Application.WorkPackages;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// Read-only context tools that let the agent target the right project / work package / contractor /
/// unit before writing. Thin wrappers over the application-service ports — no domain or EF logic.
/// </summary>
[McpServerToolType]
public static class ContextTools
{
    [McpServerTool(Name = "list_projects"), Description(
        "List all projects (the duplex builds being tracked). Call this first to find the project id " +
        "you need before listing its work packages.")]
    public static async Task<IReadOnlyList<ProjectDto>> ListProjects(
        IProjectAppService service, CancellationToken ct)
        => await service.ListAsync(ct);

    [McpServerTool(Name = "list_work_packages"), Description(
        "List the work packages (procurement units, e.g. La Roșu, Tâmplărie) under a project. Use this " +
        "to resolve the workPackageId a bid is opened against.")]
    public static async Task<IReadOnlyList<WorkPackageDto>> ListWorkPackages(
        IWorkPackageAppService service,
        [Description("The project id (from list_projects).")] Guid projectId,
        CancellationToken ct)
        => await service.ListByProjectAsync(projectId, ct);

    [McpServerTool(Name = "list_contractors"), Description(
        "List all registered contractors (firms). ALWAYS call this before register_contractor to check " +
        "the contractor isn't already registered and avoid creating a duplicate.")]
    public static async Task<IReadOnlyList<ContractorDto>> ListContractors(
        IContractorAppService service, CancellationToken ct)
        => await service.ListAsync(ct);

    [McpServerTool(Name = "list_units_of_measure"), Description(
        "List the active canonical units of measure with their codes and aliases (e.g. m³ ← mc, m² ← mp, " +
        "pcs ← buc). Call this before add_boq_line_items so you know which unit tokens will resolve, and " +
        "to get the unitOfMeasureId needed by revise_boq_line_item.")]
    public static async Task<IReadOnlyList<UnitOfMeasureDto>> ListUnitsOfMeasure(
        IUnitOfMeasureAppService service, CancellationToken ct)
        => await service.ListAsync(includeInactive: false, ct);
}
