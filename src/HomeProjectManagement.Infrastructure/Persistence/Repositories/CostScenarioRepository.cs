using HomeProjectManagement.Domain.CostScenarios;
using HomeProjectManagement.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="ICostScenarioRepository"/>.</summary>
public sealed class CostScenarioRepository(AppDbContext db) : ICostScenarioRepository
{
    public async Task<CostScenario?> GetAsync(CostScenarioId id, CancellationToken cancellationToken = default) =>
        await db.CostScenarios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CostScenario>> ListByProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default) =>
        await db.CostScenarios
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.CreatedOn)
            .ToListAsync(cancellationToken);

    public void Add(CostScenario root) => db.CostScenarios.Add(root);

    public void Remove(CostScenario root) => db.CostScenarios.Remove(root);
}
