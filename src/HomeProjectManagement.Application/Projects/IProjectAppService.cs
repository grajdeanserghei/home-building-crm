namespace HomeProjectManagement.Application.Projects;

/// <summary>
/// Driving (primary) port for project use cases. The minimal-API endpoints in ApiService
/// are the adapter that calls this; the host never touches EF Core or the domain directly.
/// </summary>
public interface IProjectAppService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ProjectDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectDto> CreateAsync(CreateProjectCommand command, CancellationToken cancellationToken = default);

    Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
