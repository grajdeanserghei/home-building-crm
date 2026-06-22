using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Projects.Events;

/// <summary>Raised when a project transitions from one status to another.</summary>
public sealed record ProjectStatusChanged(
    ProjectId ProjectId,
    ProjectStatus From,
    ProjectStatus To,
    DateTimeOffset OccurredOn) : IDomainEvent;
