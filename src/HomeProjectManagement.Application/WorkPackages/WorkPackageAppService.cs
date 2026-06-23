using HomeProjectManagement.Application.Trades;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.Trades;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.WorkPackages;

/// <summary>
/// Thin orchestration over the <see cref="WorkPackage"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. Audit fields are stamped
/// inside the unit of work from the current user + clock.
/// </summary>
public sealed class WorkPackageAppService(
    IWorkPackageRepository repository,
    IProjectRepository projects,
    ITradeRepository trades,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IWorkPackageAppService
{
    public async Task<IReadOnlyList<WorkPackageDto>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var workPackages = await repository.ListByProjectAsync(new ProjectId(projectId), cancellationToken);
        return workPackages.Select(ToDto).ToList();
    }

    public async Task<WorkPackageDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        return workPackage is null ? null : ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> DefineAsync(
        Guid projectId,
        DefineWorkPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify the owning project exists before defining a package under it.
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        // Validate the required trades against the shared vocabulary before tagging the package.
        var tradeIds = await TradeAssignment.ResolveAsync(trades, command.RequiredTradeIds, cancellationToken);

        var workPackage = WorkPackage.Define(
            project.Id,
            command.Name,
            timeProvider.GetUtcNow(),
            command.Description,
            command.Sequence,
            command.PlannedStartDate,
            command.PlannedEndDate);

        workPackage.SetRequiredTrades(tradeIds);

        repository.Add(workPackage);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> UpdateAsync(
        Guid id,
        UpdateWorkPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        workPackage.Rename(command.Name);
        workPackage.Describe(command.Description);
        workPackage.Reorder(command.Sequence);
        workPackage.Reschedule(command.PlannedStartDate, command.PlannedEndDate);

        // Required trades are a set-replace, but only when supplied: a null RequiredTradeIds leaves
        // the existing set untouched (so the edit form, which manages trades incrementally elsewhere,
        // doesn't clobber them); a non-null list — including an empty one — replaces the whole set.
        if (command.RequiredTradeIds is not null)
        {
            workPackage.SetRequiredTrades(
                await TradeAssignment.ResolveAsync(trades, command.RequiredTradeIds, cancellationToken));
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> AddRequiredTradeAsync(
        Guid id,
        Guid tradeId,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        // Validate the trade exists and is active before requiring it.
        var resolved = await TradeAssignment.ResolveAsync(trades, [tradeId], cancellationToken);
        workPackage.RequireTrade(resolved[0]);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> RemoveRequiredTradeAsync(
        Guid id,
        Guid tradeId,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        // Removal is idempotent and needs no vocabulary check — a now-retired trade can still be dropped.
        workPackage.RemoveRequiredTrade(new TradeId(tradeId));

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> ChangeStatusAsync(
        Guid id,
        ChangeWorkPackageStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        // Dispatch to the matching intention-revealing domain method so its invariant holds.
        // Defined (reopening scope) goes through the generic transition; Awarded is rejected
        // there (it must go through the award flow) and surfaces as a 409.
        switch (command.Status)
        {
            case WorkPackageStatus.OpenForBids:
                workPackage.OpenForBids(now);
                break;
            case WorkPackageStatus.InProgress:
                workPackage.Start(now);
                break;
            case WorkPackageStatus.Completed:
                workPackage.Complete(now);
                break;
            case WorkPackageStatus.Cancelled:
                workPackage.Cancel(now);
                break;
            default:
                workPackage.ChangeStatus(command.Status, now);
                break;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> AddScopeItemAsync(
        Guid id,
        ScopeItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return null;
        }

        workPackage.AddScopeItem(
            command.Name,
            command.Requirement,
            timeProvider.GetUtcNow(),
            command.Description,
            command.Sequence);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<WorkPackageDto?> UpdateScopeItemAsync(
        Guid id,
        Guid scopeItemId,
        ScopeItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null ||
            !workPackage.UpdateScopeItem(
                new ScopeItemId(scopeItemId),
                command.Name,
                command.Requirement,
                command.Description,
                command.Sequence))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
    }

    public async Task<bool> RemoveScopeItemAsync(
        Guid id,
        Guid scopeItemId,
        CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null || !workPackage.RemoveScopeItem(new ScopeItemId(scopeItemId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workPackage = await repository.GetAsync(new WorkPackageId(id), cancellationToken);
        if (workPackage is null)
        {
            return false;
        }

        repository.Remove(workPackage);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static WorkPackageDto ToDto(WorkPackage workPackage) => new(
        workPackage.Id.Value,
        workPackage.ProjectId.Value,
        workPackage.Name,
        workPackage.Description,
        workPackage.Status,
        workPackage.Sequence,
        workPackage.PlannedStartDate,
        workPackage.PlannedEndDate,
        workPackage.AwardedContractId?.Value,
        workPackage.ScopeItems
            .OrderBy(si => si.Sequence)
            .Select(ToDto)
            .ToList(),
        workPackage.RequiredTradeIds.Select(t => t.Value).ToArray(),
        workPackage.CreatedOn);

    private static ScopeItemDto ToDto(ScopeItem scopeItem) => new(
        scopeItem.Id.Value,
        scopeItem.Name,
        scopeItem.Description,
        scopeItem.Requirement,
        scopeItem.Sequence);
}
