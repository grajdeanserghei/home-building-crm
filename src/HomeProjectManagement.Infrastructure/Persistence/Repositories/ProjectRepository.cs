using HomeProjectManagement.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="IProjectRepository"/>.</summary>
public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default) =>
        await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Projects.OrderByDescending(p => p.CreatedOn).ToListAsync(cancellationToken);

    public void Add(Project root) => db.Projects.Add(root);

    public void Remove(Project root) => db.Projects.Remove(root);
}
