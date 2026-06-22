using HomeProjectManagement.Application.Bids;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for bids: thin minimal-API endpoints that call
/// <see cref="IBidAppService"/> and return DTOs. The competing-bids collection is nested under
/// its work package; an individual bid is a root, addressable by its own id, with sub-resources
/// for its discussion log. Domain rule violations (duplicate pair, illegal transition) are raised as
/// domain exceptions and turned into ProblemDetails (validation → 400, conflict → 409) by the global
/// exception handler, so the endpoints stay thin.
/// </summary>
public static class BidEndpoints
{
    public static IEndpointRouteBuilder MapBidEndpoints(this IEndpointRouteBuilder app)
    {
        // Work-package-scoped collection: list and open a bid within a work package.
        var byWorkPackage = app.MapGroup("/api/work-packages/{workPackageId:guid}/bids");

        byWorkPackage.MapGet("/",
            async (Guid workPackageId, IBidAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListByWorkPackageAsync(workPackageId, ct)));

        byWorkPackage.MapPost("/",
            async (Guid workPackageId, OpenBidCommand command, IBidAppService service, CancellationToken ct) =>
                await service.OpenAsync(workPackageId, command, ct) is { } created
                    ? Results.Created($"/api/bids/{created.Id}", created)
                    : Results.NotFound());

        // Contractor-scoped read: every bid a contractor has participated in.
        app.MapGet("/api/contractors/{contractorId:guid}/bids",
            async (Guid contractorId, IBidAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListByContractorAsync(contractorId, ct)));

        // Root-level item: a bid is an aggregate root, addressable by its own id.
        var bids = app.MapGroup("/api/bids");

        bids.MapGet("/{id:guid}",
            async (Guid id, IBidAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } bid
                    ? Results.Ok(bid)
                    : Results.NotFound());

        bids.MapPut("/{id:guid}",
            async (Guid id, UpdateBidCommand command, IBidAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        bids.MapPost("/{id:guid}/status",
            async (Guid id, ChangeBidStatusCommand command, IBidAppService service, CancellationToken ct) =>
                await service.ChangeStatusAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        bids.MapDelete("/{id:guid}",
            async (Guid id, IBidAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Discussion log: notes are sub-resources of a bid (internal entities of the aggregate).
        bids.MapPost("/{id:guid}/notes",
            async (Guid id, LogDiscussionNoteCommand command, IBidAppService service, CancellationToken ct) =>
                await service.LogNoteAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        bids.MapDelete("/{id:guid}/notes/{noteId:guid}",
            async (Guid id, Guid noteId, IBidAppService service, CancellationToken ct) =>
                await service.RemoveNoteAsync(id, noteId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
