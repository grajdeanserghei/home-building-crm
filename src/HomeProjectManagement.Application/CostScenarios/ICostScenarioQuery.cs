namespace HomeProjectManagement.Application.CostScenarios;

/// <summary>
/// Read/query use case (driving port) that composes a cost scenario's price picture across aggregates
/// — the chosen bids, their bills of quantities, and the project's apartment-unit scaling. It is
/// read-only: it loads through the existing repository ports and never mutates or commits. The "which
/// figure counts and how it sums" rule lives in the one implementation, shared with the candidate
/// listing the editor uses to choose bids.
/// </summary>
public interface ICostScenarioQuery
{
    /// <summary>
    /// The computed cost picture for one scenario, or null if the scenario (or its project) does not
    /// exist (→ 404 at the edge).
    /// </summary>
    Task<CostScenarioResultDto?> GetAsync(Guid scenarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The priced bids available to choose from, per work package, for a project — or null if the
    /// project does not exist (→ 404). Every work package is listed (with a possibly empty bid set).
    /// </summary>
    Task<IReadOnlyList<ScenarioCandidateWorkPackageDto>?> GetCandidatesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
