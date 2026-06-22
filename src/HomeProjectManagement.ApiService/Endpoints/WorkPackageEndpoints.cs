using HomeProjectManagement.Application.WorkPackages;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for work packages: thin minimal-API endpoints that call
/// <see cref="IWorkPackageAppService"/> and return DTOs. The collection is nested under its
/// owning project; an individual package is a root, addressable by its own id.
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

        return app;
    }
}
