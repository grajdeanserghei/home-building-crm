using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Contracts.Events;

/// <summary>
/// Raised when a contract is awarded for a work package — created from the selected bid's accepted
/// Bill of Quantities. The matching transition of the work package to <c>Awarded</c> is coordinated
/// by an application service.
/// </summary>
public sealed record ContractAwarded(
    ContractId ContractId,
    WorkPackageId WorkPackageId,
    BoqId AcceptedBoqId,
    DateTimeOffset OccurredOn) : IDomainEvent;
