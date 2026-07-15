using HomeProjectManagement.Application.ValuationCatalogs;
using HomeProjectManagement.Application.Valuations;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for the appraiser's valuation catalog: thin minimal-API endpoints that
/// call <see cref="IValuationCatalogAppService"/> (and the live estimate-vs-BoQ read model) and return
/// DTOs. A project has at most one catalog, reachable under the project; the catalog is a root addressable
/// by its own id, with sub-resources for its items and their BoQ mappings. Domain-rule violations (a second
/// catalog for a project, currency mismatch, a double-counted or granularity-conflicting mapping) surface
/// as domain exceptions turned into ProblemDetails (validation → 400, conflict → 409) by the global handler.
/// </summary>
public static class ValuationCatalogEndpoints
{
    public static IEndpointRouteBuilder MapValuationCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        // Project-scoped: get or create the single catalog for a project.
        var byProject = app.MapGroup("/api/projects/{projectId:guid}/valuation-catalog");

        byProject.MapGet("/",
            async (Guid projectId, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.GetByProjectAsync(projectId, ct) is { } catalog
                    ? Results.Ok(catalog)
                    : Results.NotFound());

        byProject.MapPost("/",
            async (Guid projectId, CreateValuationCatalogCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.CreateAsync(projectId, command, ct) is { } created
                    ? Results.Created($"/api/valuation-catalogs/{created.Id}", created)
                    : Results.NotFound());

        // Live estimate-vs-real-BoQ comparison for the project's catalog. The competing BoQs of each work
        // package collapse to the one that is "real" under the basis; only 'decided' (the default) applies
        // here — a scenario basis has its own scenario-scoped route.
        app.MapGet("/api/projects/{projectId:guid}/valuation/comparison",
            async (Guid projectId, string? basis, IValuationVsBoqQuery query, CancellationToken ct) =>
                await query.GetAsync(projectId, new ComparisonBasis.Decided(), ct) is { } comparison
                    ? Results.Ok(comparison)
                    : Results.NotFound());

        // Root-level item: the catalog is an aggregate root, addressable by its own id.
        var catalogs = app.MapGroup("/api/valuation-catalogs");

        catalogs.MapGet("/{id:guid}",
            async (Guid id, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } catalog
                    ? Results.Ok(catalog)
                    : Results.NotFound());

        catalogs.MapPut("/{id:guid}/header",
            async (Guid id, UpdateValuationCatalogHeaderCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.UpdateHeaderAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        catalogs.MapPost("/{id:guid}/activate",
            async (Guid id, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.ActivateAsync(id, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        catalogs.MapPut("/{id:guid}/vat-rate",
            async (Guid id, ChangeVatRateCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.ChangeVatRateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        catalogs.MapDelete("/{id:guid}",
            async (Guid id, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Items: priced rows of the estimate.
        catalogs.MapPost("/{id:guid}/items",
            async (Guid id, AddValuationItemCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.AddItemAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        catalogs.MapPut("/{id:guid}/items/{itemId:guid}",
            async (Guid id, Guid itemId, ReviseValuationItemCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.ReviseItemAsync(id, itemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        catalogs.MapPost("/{id:guid}/items/{itemId:guid}/deactivate",
            async (Guid id, Guid itemId, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.DeactivateItemAsync(id, itemId, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // BoQ mappings: link a catalog item to a real BoQ section (or subsection).
        catalogs.MapPost("/{id:guid}/items/{itemId:guid}/links",
            async (Guid id, Guid itemId, LinkBoqSectionCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.LinkBoqSectionAsync(id, itemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // Unlink is a POST (not DELETE) because the mapping to remove is identified by a body tuple, and
        // minimal APIs reject an inferred request body on DELETE.
        catalogs.MapPost("/{id:guid}/items/{itemId:guid}/links/remove",
            async (Guid id, Guid itemId, LinkBoqSectionCommand command, IValuationCatalogAppService service, CancellationToken ct) =>
                await service.UnlinkBoqSectionAsync(id, itemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        return app;
    }
}
