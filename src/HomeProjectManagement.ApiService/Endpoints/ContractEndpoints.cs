using HomeProjectManagement.Application.Contracts;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for contracts: thin minimal-API endpoints that call
/// <see cref="IContractAppService"/> and return DTOs. A contract is an aggregate root addressable by
/// its own id; it is awarded from an accepted BoQ and there is at most one per work package (also
/// reachable via <c>/api/work-packages/{id}/contract</c>). Invariant violations the service surfaces
/// as <see cref="InvalidOperationException"/> (awarding from a non-accepted BoQ or non-selected bid,
/// a work package already under contract, an illegal status transition) map to 409.
/// </summary>
public static class ContractEndpoints
{
    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var contracts = app.MapGroup("/api/contracts");

        contracts.MapGet("/",
            async (IContractAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(ct)));

        contracts.MapGet("/{id:guid}",
            async (Guid id, IContractAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } contract
                    ? Results.Ok(contract)
                    : Results.NotFound());

        // Award a contract from an accepted BoQ (the work package is reached through its bid).
        contracts.MapPost("/",
            async (AwardContractCommand command, IContractAppService service, CancellationToken ct) =>
            {
                try
                {
                    return await service.AwardAsync(command, ct) is { } created
                        ? Results.Created($"/api/contracts/{created.Id}", created)
                        : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            });

        contracts.MapPut("/{id:guid}",
            async (Guid id, UpdateContractCommand command, IContractAppService service, CancellationToken ct) =>
            {
                try
                {
                    return await service.UpdateAsync(id, command, ct) is { } updated
                        ? Results.Ok(updated)
                        : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            });

        contracts.MapPost("/{id:guid}/status",
            async (Guid id, ChangeContractStatusCommand command, IContractAppService service, CancellationToken ct) =>
            {
                try
                {
                    return await service.ChangeStatusAsync(id, command, ct) is { } updated
                        ? Results.Ok(updated)
                        : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            });

        contracts.MapDelete("/{id:guid}",
            async (Guid id, IContractAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Convenience lookup: the (at most one) contract awarded for a work package.
        app.MapGet("/api/work-packages/{workPackageId:guid}/contract",
            async (Guid workPackageId, IContractAppService service, CancellationToken ct) =>
                await service.GetByWorkPackageAsync(workPackageId, ct) is { } contract
                    ? Results.Ok(contract)
                    : Results.NotFound());

        return app;
    }
}
