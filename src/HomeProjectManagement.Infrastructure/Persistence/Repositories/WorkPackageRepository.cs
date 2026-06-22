using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="IWorkPackageRepository"/>.</summary>
public sealed class WorkPackageRepository(AppDbContext db) : IWorkPackageRepository
{
    public async Task<WorkPackage?> GetAsync(WorkPackageId id, CancellationToken cancellationToken = default) =>
        await db.WorkPackages.FirstOrDefaultAsync(wp => wp.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkPackage>> ListByProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default) =>
        await db.WorkPackages
            .Where(wp => wp.ProjectId == projectId)
            .OrderBy(wp => wp.Sequence)
            .ToListAsync(cancellationToken);

    public void Add(WorkPackage root) => db.WorkPackages.Add(root);

    public void Remove(WorkPackage root) => db.WorkPackages.Remove(root);
}
