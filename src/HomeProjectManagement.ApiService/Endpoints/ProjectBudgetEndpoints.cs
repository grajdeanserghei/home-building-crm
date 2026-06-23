using HomeProjectManagement.Application.Budgeting;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for the project budget read model: a single thin minimal-API
/// endpoint that calls <see cref="IProjectBudgetQuery"/> and returns the composed DTO. The rollup
/// across work packages, bids, BoQs and contracts happens in the application layer, not here.
/// </summary>
public static class ProjectBudgetEndpoints
{
    public static IEndpointRouteBuilder MapProjectBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        // The cost rollup for one project: per-work-package figures plus per-currency projection.
        app.MapGet("/api/projects/{projectId:guid}/budget",
            async (Guid projectId, IProjectBudgetQuery query, CancellationToken ct) =>
                await query.GetAsync(projectId, ct) is { } budget
                    ? Results.Ok(budget)
                    : Results.NotFound());

        return app;
    }
}
