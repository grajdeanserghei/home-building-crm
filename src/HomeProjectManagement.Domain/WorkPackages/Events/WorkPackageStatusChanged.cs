using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.WorkPackages.Events;

/// <summary>Raised when a work package transitions from one status to another.</summary>
public sealed record WorkPackageStatusChanged(
    WorkPackageId WorkPackageId,
    WorkPackageStatus From,
    WorkPackageStatus To,
    DateTimeOffset OccurredOn) : IDomainEvent;
