using HomeProjectManagement.Application.ConstructionValuations;
using HomeProjectManagement.Application.Valuations;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for dated construction-valuation snapshots: thin minimal-API endpoints
/// that call <see cref="IConstructionValuationAppService"/> (capture/list) and the frozen progress read
/// model. Snapshots are captured against a catalog (idempotent by source content hash) and are individually
/// addressable by id. Domain-rule violations surface as ProblemDetails via the global exception handler.
/// </summary>
public static class ConstructionValuationEndpoints
{
    public static IEndpointRouteBuilder MapConstructionValuationEndpoints(this IEndpointRouteBuilder app)
    {
        // Catalog-scoped: capture and list the dated snapshots for a catalog.
        var byCatalog = app.MapGroup("/api/valuation-catalogs/{catalogId:guid}/valuations");

        byCatalog.MapGet("/",
            async (Guid catalogId, IConstructionValuationAppService service, CancellationToken ct) =>
                await service.ListByCatalogAsync(catalogId, ct) is { } list
                    ? Results.Ok(list)
                    : Results.NotFound());

        byCatalog.MapPost("/",
            async (Guid catalogId, CaptureConstructionValuationCommand command, IConstructionValuationAppService service, CancellationToken ct) =>
                await service.CaptureAsync(catalogId, command, ct) is { } captured
                    ? Results.Created($"/api/construction-valuations/{captured.Id}", captured)
                    : Results.NotFound());

        // Completion-progress series across a catalog's snapshots (frozen).
        byCatalog.MapGet("/progress",
            async (Guid catalogId, IValuationProgressQuery query, CancellationToken ct) =>
                await query.GetSeriesAsync(catalogId, ct) is { } series
                    ? Results.Ok(series)
                    : Results.NotFound());

        // Root-level item: one frozen snapshot, addressable by its own id.
        app.MapGet("/api/construction-valuations/{id:guid}",
            async (Guid id, IConstructionValuationAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } valuation
                    ? Results.Ok(valuation)
                    : Results.NotFound());

        return app;
    }
}
