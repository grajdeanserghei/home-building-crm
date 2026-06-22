namespace HomeProjectManagement.ApiService.Models;

public class Project
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}

public enum ProjectStatus
{
    Planned,
    InProgress,
    OnHold,
    Completed
}
