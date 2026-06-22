using HomeProjectManagement.Application.UnitsOfMeasure;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for units of measure: thin minimal-API endpoints that call
/// <see cref="IUnitOfMeasureAppService"/> and return DTOs. A unit is shared reference data — an
/// aggregate root addressable by its own id, not nested under any project.
/// </summary>
/// <remarks>
/// There is intentionally no delete endpoint: a unit referenced by historical line items must
/// not vanish, so it is <b>retired via deactivate</b> instead of deleted.
/// </remarks>
public static class UnitOfMeasureEndpoints
{
    public static IEndpointRouteBuilder MapUnitOfMeasureEndpoints(this IEndpointRouteBuilder app)
    {
        var units = app.MapGroup("/api/units-of-measure");

        units.MapGet("/",
            async (bool? includeInactive, IUnitOfMeasureAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(includeInactive ?? true, ct)));

        units.MapGet("/{id:guid}",
            async (Guid id, IUnitOfMeasureAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } unit
                    ? Results.Ok(unit)
                    : Results.NotFound());

        units.MapPost("/",
            async (DefineUnitOfMeasureCommand command, IUnitOfMeasureAppService service, CancellationToken ct) =>
            {
                var result = await service.DefineAsync(command, ct);
                return result.Created is { } created
                    ? Results.Created($"/api/units-of-measure/{created.Id}", created)
                    : Results.Conflict($"A unit of measure with code '{result.ConflictCode}' already exists.");
            });

        units.MapPut("/{id:guid}",
            async (Guid id, UpdateUnitOfMeasureCommand command, IUnitOfMeasureAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        units.MapPost("/{id:guid}/activate",
            async (Guid id, IUnitOfMeasureAppService service, CancellationToken ct) =>
                await service.SetActiveAsync(id, true, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        units.MapPost("/{id:guid}/deactivate",
            async (Guid id, IUnitOfMeasureAppService service, CancellationToken ct) =>
                await service.SetActiveAsync(id, false, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
