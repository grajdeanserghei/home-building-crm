using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.CostScenarios;

/// <summary>
/// Persistence port for the <see cref="CostScenario"/> aggregate (driven port; implemented by EF
/// Core in Infrastructure). Lives beside the aggregate it serves. Repositories return whole
/// scenarios, including their selections.
/// </summary>
public interface ICostScenarioRepository : IRepository<CostScenario, CostScenarioId>
{
    /// <summary>A project's cost scenarios, oldest first.</summary>
    Task<IReadOnlyList<CostScenario>> ListByProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default);
}
