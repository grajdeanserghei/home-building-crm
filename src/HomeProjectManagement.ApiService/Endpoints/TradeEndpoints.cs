using HomeProjectManagement.Application.Trades;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for trades: thin minimal-API endpoints that call
/// <see cref="ITradeAppService"/> and return DTOs. A trade is shared, project-independent reference
/// data — an aggregate root addressable by its own id, referenced by contractors and work packages.
/// </summary>
/// <remarks>
/// There is intentionally no delete endpoint: a trade referenced by contractors or work packages
/// must not vanish, so it is <b>retired via deactivate</b> instead of deleted.
/// </remarks>
public static class TradeEndpoints
{
    public static IEndpointRouteBuilder MapTradeEndpoints(this IEndpointRouteBuilder app)
    {
        var trades = app.MapGroup("/api/trades");

        trades.MapGet("/",
            async (bool? includeInactive, ITradeAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(includeInactive ?? true, ct)));

        trades.MapGet("/{id:guid}",
            async (Guid id, ITradeAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } trade
                    ? Results.Ok(trade)
                    : Results.NotFound());

        trades.MapPost("/",
            async (DefineTradeCommand command, ITradeAppService service, CancellationToken ct) =>
            {
                var result = await service.DefineAsync(command, ct);
                return result.Created is { } created
                    ? Results.Created($"/api/trades/{created.Id}", created)
                    : Results.Conflict($"A trade named '{result.ConflictName}' already exists.");
            });

        trades.MapPut("/{id:guid}",
            async (Guid id, UpdateTradeCommand command, ITradeAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        trades.MapPost("/{id:guid}/activate",
            async (Guid id, ITradeAppService service, CancellationToken ct) =>
                await service.SetActiveAsync(id, true, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        trades.MapPost("/{id:guid}/deactivate",
            async (Guid id, ITradeAppService service, CancellationToken ct) =>
                await service.SetActiveAsync(id, false, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
