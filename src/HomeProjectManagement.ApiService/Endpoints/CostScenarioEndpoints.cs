using HomeProjectManagement.Application.CostScenarios;
using HomeProjectManagement.Application.Valuations;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for cost scenarios: thin minimal-API endpoints that call
/// <see cref="ICostScenarioAppService"/> (mutations + summaries) and <see cref="ICostScenarioQuery"/>
/// (the computed cost picture and the editor's candidate listing). A project's scenarios are nested
/// under the project; an individual scenario is a root, addressable by its own id, with its selections
/// as sub-resources. Domain rule violations are turned into ProblemDetails (validation → 400) by the
/// global exception handler, so the endpoints stay thin.
/// </summary>
public static class CostScenarioEndpoints
{
    public static IEndpointRouteBuilder MapCostScenarioEndpoints(this IEndpointRouteBuilder app)
    {
        // Project-scoped collection: list, create, and the per-work-package candidate bids.
        var byProject = app.MapGroup("/api/projects/{projectId:guid}/cost-scenarios");

        byProject.MapGet("/",
            async (Guid projectId, ICostScenarioAppService service, CancellationToken ct) =>
                Results.Ok(await service.ListByProjectAsync(projectId, ct)));

        byProject.MapPost("/",
            async (Guid projectId, CreateCostScenarioCommand command, ICostScenarioAppService service, CancellationToken ct) =>
                await service.CreateAsync(projectId, command, ct) is { } created
                    ? Results.Created($"/api/cost-scenarios/{created.Id}", created)
                    : Results.NotFound());

        byProject.MapGet("/candidates",
            async (Guid projectId, ICostScenarioQuery query, CancellationToken ct) =>
                await query.GetCandidatesAsync(projectId, ct) is { } candidates
                    ? Results.Ok(candidates)
                    : Results.NotFound());

        // Root-level item: a scenario is an aggregate root, addressable by its own id.
        var scenarios = app.MapGroup("/api/cost-scenarios");

        // The headline read: the computed cost picture (per-line breakdown + per-currency totals).
        scenarios.MapGet("/{id:guid}",
            async (Guid id, ICostScenarioQuery query, CancellationToken ct) =>
                await query.GetAsync(id, ct) is { } result
                    ? Results.Ok(result)
                    : Results.NotFound());

        // Estimate-vs-real under this scenario's chosen bids (the simulator's what-if basis). The project
        // and its catalog are resolved from the scenario; competing BoQs collapse to the scenario's picks.
        scenarios.MapGet("/{id:guid}/valuation-comparison",
            async (Guid id, ICostScenarioQuery scenarioQuery, IValuationVsBoqQuery valuationQuery, CancellationToken ct) =>
            {
                var scenario = await scenarioQuery.GetAsync(id, ct);
                if (scenario is null)
                {
                    return Results.NotFound();
                }

                return await valuationQuery.GetAsync(scenario.ProjectId, new ComparisonBasis.Scenario(id), ct) is { } comparison
                    ? Results.Ok(comparison)
                    : Results.NotFound();
            });

        scenarios.MapPut("/{id:guid}",
            async (Guid id, UpdateCostScenarioCommand command, ICostScenarioAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        scenarios.MapDelete("/{id:guid}",
            async (Guid id, ICostScenarioAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Selections: the chosen bid per work package (internal entities of the aggregate).
        scenarios.MapPost("/{id:guid}/selections",
            async (Guid id, IncludeBidCommand command, ICostScenarioAppService service, CancellationToken ct) =>
                await service.IncludeBidAsync(id, command, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        scenarios.MapDelete("/{id:guid}/work-packages/{workPackageId:guid}",
            async (Guid id, Guid workPackageId, ICostScenarioAppService service, CancellationToken ct) =>
                await service.RemoveWorkPackageAsync(id, workPackageId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
