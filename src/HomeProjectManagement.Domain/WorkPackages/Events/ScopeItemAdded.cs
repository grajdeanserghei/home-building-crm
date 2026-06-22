using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.WorkPackages.Events;

/// <summary>Raised when an owner-defined scope item is added to a work package.</summary>
public sealed record ScopeItemAdded(
    WorkPackageId WorkPackageId,
    ScopeItemId ScopeItemId,
    string Name,
    ScopeItemRequirement Requirement,
    DateTimeOffset OccurredOn) : IDomainEvent;
