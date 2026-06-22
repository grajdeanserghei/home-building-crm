using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects.Events;

namespace HomeProjectManagement.Domain.Projects;

/// <summary>
/// The duplex build being tracked — the top-level aggregate everything else hangs off.
/// Encapsulated: no public setters. State changes go through intention-revealing methods
/// that enforce invariants; construction goes through the <see cref="Create"/> factory.
/// </summary>
public sealed class Project : AggregateRoot<ProjectId>
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }

    /// <summary>When the build starts/started. Optional.</summary>
    public DateTimeOffset? StartDate { get; private set; }

    /// <summary>The overall target date (the domain name for the UI's "due date"). Optional.</summary>
    public DateTimeOffset? TargetCompletionDate { get; private set; }

    /// <summary>The build location. Optional.</summary>
    public Address? SiteAddress { get; private set; }

    // EF Core materialisation constructor.
    private Project()
    {
    }

    private Project(ProjectId id, string name) : base(id)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Factory: create a new project, validating its invariants. <paramref name="now"/> is
    /// supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain.
    /// </summary>
    public static Project Create(
        string name,
        DateTimeOffset now,
        string? description = null,
        ProjectStatus status = ProjectStatus.Planned,
        DateTimeOffset? startDate = null,
        DateTimeOffset? targetCompletionDate = null,
        Address? siteAddress = null)
    {
        var project = new Project(ProjectId.New(), NormalizeName(name))
        {
            Description = Trim(description),
            Status = status,
            StartDate = startDate,
            TargetCompletionDate = targetCompletionDate,
            SiteAddress = siteAddress
        };

        EnsureDatesConsistent(project.StartDate, project.TargetCompletionDate);
        project.Raise(new ProjectCreated(project.Id, project.Name, now));
        return project;
    }

    /// <summary>Rename the project.</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Update the free-text description.</summary>
    public void Describe(string? description) => Description = Trim(description);

    /// <summary>Transition the project to a new status, raising an event if it changed.</summary>
    public void ChangeStatus(ProjectStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        var previous = Status;
        Status = status;
        Raise(new ProjectStatusChanged(Id, previous, status, now));
    }

    /// <summary>Set or clear the planned start and target completion dates.</summary>
    public void Reschedule(DateTimeOffset? startDate, DateTimeOffset? targetCompletionDate)
    {
        EnsureDatesConsistent(startDate, targetCompletionDate);
        StartDate = startDate;
        TargetCompletionDate = targetCompletionDate;
    }

    /// <summary>Set or clear the build's site address.</summary>
    public void RelocateSite(Address? siteAddress) => SiteAddress = siteAddress;

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureDatesConsistent(DateTimeOffset? start, DateTimeOffset? target)
    {
        if (start is not null && target is not null && target < start)
        {
            throw new ArgumentException("Target completion date must not be before the start date.");
        }
    }
}
