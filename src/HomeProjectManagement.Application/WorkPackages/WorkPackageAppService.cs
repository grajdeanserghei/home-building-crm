using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;
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

        var workPackage = WorkPackage.Define(
            project.Id,
            command.Name,
            timeProvider.GetUtcNow(),
            command.Description,
            command.Sequence,
            command.PlannedStartDate,
            command.PlannedEndDate);

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

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(workPackage);
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
        workPackage.CreatedOn);
}
