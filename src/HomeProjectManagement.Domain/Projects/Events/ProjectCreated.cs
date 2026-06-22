using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Projects.Events;

/// <summary>Raised when a new project is created.</summary>
public sealed record ProjectCreated(ProjectId ProjectId, string Name, DateTimeOffset OccurredOn) : IDomainEvent;
