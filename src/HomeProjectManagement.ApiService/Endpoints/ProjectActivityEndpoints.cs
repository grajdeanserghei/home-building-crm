using HomeProjectManagement.Application.Activity;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for the project activity feed: a single thin minimal-API endpoint
/// that calls <see cref="IProjectActivityQuery"/> and returns the composed, newest-first feed. The
/// cross-aggregate assembly (notes, bids, work packages) happens in the application layer, not here.
/// </summary>
public static class ProjectActivityEndpoints
{
    public static IEndpointRouteBuilder MapProjectActivityEndpoints(this IEndpointRouteBuilder app)
    {
        // The recent-activity feed for one project: discussion notes plus structural additions.
        app.MapGet("/api/projects/{projectId:guid}/activity",
            async (Guid projectId, IProjectActivityQuery query, CancellationToken ct) =>
                await query.GetAsync(projectId, 30, ct) is { } feed
                    ? Results.Ok(feed)
                    : Results.NotFound());

        return app;
    }
}
