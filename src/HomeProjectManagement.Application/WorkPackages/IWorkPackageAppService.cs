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

    /// <summary>
    /// Move the package through its lifecycle (the target dispatches to the matching domain
    /// transition). Returns null if the package does not exist; an illegal transition (e.g.
    /// starting an un-awarded package, or targeting Awarded) is a domain conflict → HTTP 409.
    /// </summary>
    Task<WorkPackageDto?> ChangeStatusAsync(Guid id, ChangeWorkPackageStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>Add an owner-defined scope item. Returns null if the package does not exist.</summary>
    Task<WorkPackageDto?> AddScopeItemAsync(Guid id, ScopeItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Edit a scope item. Returns null if the package or scope item is absent.</summary>
    Task<WorkPackageDto?> UpdateScopeItemAsync(Guid id, Guid scopeItemId, ScopeItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a scope item. Returns false if the package or scope item is absent.</summary>
    Task<bool> RemoveScopeItemAsync(Guid id, Guid scopeItemId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
