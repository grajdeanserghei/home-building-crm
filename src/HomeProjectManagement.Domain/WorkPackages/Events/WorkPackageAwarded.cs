using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contracts;

namespace HomeProjectManagement.Domain.WorkPackages.Events;

/// <summary>
/// Raised when a work package is awarded — a bid was selected and a contract created from it.
/// Carries the contract id so downstream handlers (in other aggregates/contexts) can react
/// without loading the work package.
/// </summary>
public sealed record WorkPackageAwarded(
    WorkPackageId WorkPackageId,
    ContractId ContractId,
    DateTimeOffset OccurredOn) : IDomainEvent;
