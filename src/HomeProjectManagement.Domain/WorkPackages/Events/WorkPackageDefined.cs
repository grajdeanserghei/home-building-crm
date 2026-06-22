using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.WorkPackages.Events;

/// <summary>Raised when a new work package is defined within a project.</summary>
public sealed record WorkPackageDefined(
    WorkPackageId WorkPackageId,
    ProjectId ProjectId,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent;
