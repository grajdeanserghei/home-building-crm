namespace HomeProjectManagement.Application.CostScenarios;

/// <summary>
/// Use cases (driving/primary port) for managing cost scenarios — the saved, named bid combinations
/// per work package. Mutations load through the repository port, invoke domain behaviour, and commit
/// through the unit of work; the computed cost picture is served separately by
/// <see cref="ICostScenarioQuery"/>. Cross-aggregate guards (project/work-package/bid existence and
/// ownership) live here.
/// </summary>
public interface ICostScenarioAppService
{
    /// <summary>A project's scenarios as lightweight summaries.</summary>
    Task<IReadOnlyList<CostScenarioSummaryDto>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>Create a scenario in a project, or null if the project does not exist (→ 404).</summary>
    Task<CostScenarioSummaryDto?> CreateAsync(
        Guid projectId,
        CreateCostScenarioCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Edit a scenario's name/description, or null if it does not exist (→ 404).</summary>
    Task<CostScenarioSummaryDto?> UpdateAsync(
        Guid id,
        UpdateCostScenarioCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Choose a bid for a work package within the scenario (upsert). Returns false if the scenario
    /// does not exist (→ 404). Throws when the work package or bid is unknown or does not belong to
    /// the scenario's project / chosen work package (→ 400).
    /// </summary>
    Task<bool> IncludeBidAsync(
        Guid id,
        IncludeBidCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Exclude a work package from the scenario. Returns false if the scenario does not exist (→ 404).</summary>
    Task<bool> RemoveWorkPackageAsync(
        Guid id,
        Guid workPackageId,
        CancellationToken cancellationToken = default);

    /// <summary>Delete a scenario. Returns false if it does not exist (→ 404).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
