using HomeProjectManagement.Application.Contractors;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for contractors: thin minimal-API endpoints that call
/// <see cref="IContractorAppService"/> and return DTOs. A contractor is global master data —
/// an aggregate root addressable by its own id, not nested under any project.
/// </summary>
public static class ContractorEndpoints
{
    public static IEndpointRouteBuilder MapContractorEndpoints(this IEndpointRouteBuilder app)
    {
        var contractors = app.MapGroup("/api/contractors");

        contractors.MapGet("/",
            async (IContractorAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(ct)));

        contractors.MapGet("/{id:guid}",
            async (Guid id, IContractorAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } contractor
                    ? Results.Ok(contractor)
                    : Results.NotFound());

        contractors.MapPost("/",
            async (RegisterContractorCommand command, IContractorAppService service, CancellationToken ct) =>
            {
                var created = await service.RegisterAsync(command, ct);
                return Results.Created($"/api/contractors/{created.Id}", created);
            });

        contractors.MapPut("/{id:guid}",
            async (Guid id, UpdateContractorCommand command, IContractorAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        contractors.MapDelete("/{id:guid}",
            async (Guid id, IContractorAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Trades performed: tagged incrementally as sub-resources of the contractor. An unknown or
        // inactive trade surfaces as a 400 via the global exception handler.
        contractors.MapPost("/{id:guid}/trades/{tradeId:guid}",
            async (Guid id, Guid tradeId, IContractorAppService service, CancellationToken ct) =>
                await service.AddTradeAsync(id, tradeId, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        contractors.MapDelete("/{id:guid}/trades/{tradeId:guid}",
            async (Guid id, Guid tradeId, IContractorAppService service, CancellationToken ct) =>
                await service.RemoveTradeAsync(id, tradeId, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        return app;
    }
}
