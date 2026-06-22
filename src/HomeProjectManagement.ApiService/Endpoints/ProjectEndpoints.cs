using HomeProjectManagement.Application.Projects;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for projects: thin minimal-API endpoints that deserialize
/// the request, call <see cref="IProjectAppService"/>, and return a DTO. No EF Core or
/// domain logic lives here.
/// </summary>
public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var projects = app.MapGroup("/api/projects");

        projects.MapGet("/", async (IProjectAppService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        projects.MapGet("/{id:guid}", async (Guid id, IProjectAppService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } project
                ? Results.Ok(project)
                : Results.NotFound());

        projects.MapPost("/", async (CreateProjectCommand command, IProjectAppService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(command, ct);
            return Results.Created($"/api/projects/{created.Id}", created);
        });

        projects.MapPut("/{id:guid}",
            async (Guid id, UpdateProjectCommand command, IProjectAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        projects.MapDelete("/{id:guid}", async (Guid id, IProjectAppService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct)
                ? Results.NoContent()
                : Results.NotFound());

        return app;
    }
}
