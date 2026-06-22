using HomeProjectManagement.Application.WorkPackages;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for work packages: thin minimal-API endpoints that call
/// <see cref="IWorkPackageAppService"/> and return DTOs. The collection is nested under its
/// owning project; an individual package is a root, addressable by its own id, with sub-resources
/// for its scope items. Domain rule violations (e.g. an illegal lifecycle transition, a duplicate
/// scope-item name) are raised as domain exceptions and turned into ProblemDetails (validation →
/// 400, conflict → 409) by the global exception handler, so the endpoints stay thin.
/// </summary>
public static class WorkPackageEndpoints
{
    public static IEndpointRouteBuilder MapWorkPackageEndpoints(this IEndpointRouteBuilder app)
    {
        // Project-scoped collection: list and define within a project.
        var byProject = app.MapGroup("/api/projects/{projectId:guid}/work-packages");

        byProject.MapGet("/",
            async (Guid projectId, IWorkPackageAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListByProjectAsync(projectId, ct)));

        byProject.MapPost("/",
            async (Guid projectId, DefineWorkPackageCommand command, IWorkPackageAppService service, CancellationToken ct) =>
                await service.DefineAsync(projectId, command, ct) is { } created
                    ? Results.Created($"/api/work-packages/{created.Id}", created)
                    : Results.NotFound());

        // Root-level item: a work package is an aggregate root, addressable by its own id.
        var workPackages = app.MapGroup("/api/work-packages");

        workPackages.MapGet("/{id:guid}",
            async (Guid id, IWorkPackageAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } workPackage
                    ? Results.Ok(workPackage)
                    : Results.NotFound());

        workPackages.MapPut("/{id:guid}",
            async (Guid id, UpdateWorkPackageCommand command, IWorkPackageAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        workPackages.MapDelete("/{id:guid}",
            async (Guid id, IWorkPackageAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Lifecycle: transition the package's status (Defined → OpenForBids → … → Completed /
        // Cancelled). Awarding goes through the dedicated award flow, not this endpoint.
        workPackages.MapPost("/{id:guid}/status",
            async (Guid id, ChangeWorkPackageStatusCommand command, IWorkPackageAppService service, CancellationToken ct) =>
                await service.ChangeStatusAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // Scope items: owner-defined sub-scopes, addressed as sub-resources of the package.
        workPackages.MapPost("/{id:guid}/scope-items",
            async (Guid id, ScopeItemCommand command, IWorkPackageAppService service, CancellationToken ct) =>
                await service.AddScopeItemAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        workPackages.MapPut("/{id:guid}/scope-items/{scopeItemId:guid}",
            async (Guid id, Guid scopeItemId, ScopeItemCommand command, IWorkPackageAppService service, CancellationToken ct) =>
                await service.UpdateScopeItemAsync(id, scopeItemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        workPackages.MapDelete("/{id:guid}/scope-items/{scopeItemId:guid}",
            async (Guid id, Guid scopeItemId, IWorkPackageAppService service, CancellationToken ct) =>
                await service.RemoveScopeItemAsync(id, scopeItemId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
