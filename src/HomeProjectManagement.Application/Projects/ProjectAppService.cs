using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Application.Projects;

/// <summary>
/// Thin orchestration over the <see cref="Project"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. Audit fields are
/// stamped inside the unit of work from the current user + clock.
/// </summary>
public sealed class ProjectAppService(
    IProjectRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IProjectAppService
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var projects = await repository.ListAsync(cancellationToken);
        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await repository.GetAsync(new ProjectId(id), cancellationToken);
        return project is null ? null : ToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = Project.Create(
            command.Name,
            timeProvider.GetUtcNow(),
            command.Description,
            command.Status,
            targetCompletionDate: command.DueDate,
            apartmentUnits: command.ApartmentUnits);

        repository.Add(project);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(project);
    }

    public async Task<ProjectDto?> UpdateAsync(
        Guid id,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await repository.GetAsync(new ProjectId(id), cancellationToken);
        if (project is null)
        {
            return null;
        }

        project.Rename(command.Name);
        project.Describe(command.Description);
        project.ChangeStatus(command.Status, timeProvider.GetUtcNow());
        project.Reschedule(project.StartDate, command.DueDate);
        project.SetApartmentUnits(command.ApartmentUnits);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(project);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await repository.GetAsync(new ProjectId(id), cancellationToken);
        if (project is null)
        {
            return false;
        }

        repository.Remove(project);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static ProjectDto ToDto(Project project) => new(
        project.Id.Value,
        project.Name,
        project.Description,
        project.Status,
        project.CreatedOn,
        project.TargetCompletionDate,
        project.ApartmentUnits);
}
