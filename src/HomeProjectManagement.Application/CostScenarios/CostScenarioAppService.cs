using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.CostScenarios;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.CostScenarios;

/// <summary>
/// Thin orchestration over the <see cref="CostScenario"/> aggregate: load via the repository port,
/// invoke domain behaviour, commit through the unit of work. The cross-aggregate guards that the
/// domain deliberately leaves out (a work package belongs to the project, a bid belongs to that work
/// package) are enforced here before the choice reaches the aggregate.
/// </summary>
public sealed class CostScenarioAppService(
    ICostScenarioRepository repository,
    IProjectRepository projects,
    IWorkPackageRepository workPackages,
    IBidRepository bids,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICostScenarioAppService
{
    public async Task<IReadOnlyList<CostScenarioSummaryDto>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var scenarios = await repository.ListByProjectAsync(new ProjectId(projectId), cancellationToken);
        return scenarios.Select(ToSummary).ToList();
    }

    public async Task<CostScenarioSummaryDto?> CreateAsync(
        Guid projectId,
        CreateCostScenarioCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        var scenario = CostScenario.Create(
            project.Id, command.Name, timeProvider.GetUtcNow(), command.Description);

        repository.Add(scenario);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToSummary(scenario);
    }

    public async Task<CostScenarioSummaryDto?> UpdateAsync(
        Guid id,
        UpdateCostScenarioCommand command,
        CancellationToken cancellationToken = default)
    {
        var scenario = await repository.GetAsync(new CostScenarioId(id), cancellationToken);
        if (scenario is null)
        {
            return null;
        }

        scenario.Rename(command.Name);
        scenario.Describe(command.Description);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToSummary(scenario);
    }

    public async Task<bool> IncludeBidAsync(
        Guid id,
        IncludeBidCommand command,
        CancellationToken cancellationToken = default)
    {
        var scenario = await repository.GetAsync(new CostScenarioId(id), cancellationToken);
        if (scenario is null)
        {
            return false;
        }

        var workPackage = await workPackages.GetAsync(new WorkPackageId(command.WorkPackageId), cancellationToken);
        if (workPackage is null || workPackage.ProjectId != scenario.ProjectId)
        {
            throw new DomainValidationException(
                "The work package does not belong to this scenario's project.",
                nameof(command.WorkPackageId));
        }

        var bid = await bids.GetAsync(new BidId(command.BidId), cancellationToken);
        if (bid is null || bid.WorkPackageId != workPackage.Id)
        {
            throw new DomainValidationException(
                "The bid does not belong to the chosen work package.",
                nameof(command.BidId));
        }

        scenario.IncludeBid(workPackage.Id, bid.Id);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveWorkPackageAsync(
        Guid id,
        Guid workPackageId,
        CancellationToken cancellationToken = default)
    {
        var scenario = await repository.GetAsync(new CostScenarioId(id), cancellationToken);
        if (scenario is null)
        {
            return false;
        }

        scenario.RemoveWorkPackage(new WorkPackageId(workPackageId));
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scenario = await repository.GetAsync(new CostScenarioId(id), cancellationToken);
        if (scenario is null)
        {
            return false;
        }

        repository.Remove(scenario);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static CostScenarioSummaryDto ToSummary(CostScenario scenario) => new(
        scenario.Id.Value,
        scenario.ProjectId.Value,
        scenario.Name,
        scenario.Selections.Count,
        scenario.CreatedOn);
}
