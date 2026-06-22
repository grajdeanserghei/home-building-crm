using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// Persistence port for the <see cref="WorkPackage"/> aggregate (driven port; implemented by
/// EF Core in Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface IWorkPackageRepository : IRepository<WorkPackage, WorkPackageId>
{
    /// <summary>The work packages of one project, in their intended order (by sequence).</summary>
    Task<IReadOnlyList<WorkPackage>> ListByProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default);
}
