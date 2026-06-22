namespace HomeProjectManagement.Application.WorkPackages;

/// <summary>
/// Driving (primary) port for work-package use cases. The minimal-API endpoints in ApiService
/// are the adapter that calls this; the host never touches EF Core or the domain directly.
/// </summary>
public interface IWorkPackageAppService
{
    /// <summary>The work packages of one project, in their intended order.</summary>
    Task<IReadOnlyList<WorkPackageDto>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<WorkPackageDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Define a package within a project. Returns null if the project does not exist.</summary>
    Task<WorkPackageDto?> DefineAsync(Guid projectId, DefineWorkPackageCommand command, CancellationToken cancellationToken = default);

    Task<WorkPackageDto?> UpdateAsync(Guid id, UpdateWorkPackageCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
