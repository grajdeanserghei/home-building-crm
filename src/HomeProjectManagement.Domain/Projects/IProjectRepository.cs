using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Projects;

/// <summary>
/// Persistence port for the <see cref="Project"/> aggregate (driven port; implemented by
/// EF Core in Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface IProjectRepository : IRepository<Project, ProjectId>
{
    /// <summary>All projects, most recently created first.</summary>
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default);
}
