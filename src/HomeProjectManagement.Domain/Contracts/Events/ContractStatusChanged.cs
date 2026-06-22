using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Contracts.Events;

/// <summary>
/// Raised when a contract transitions status (e.g. Draft → Signed → Active → Completed).
/// </summary>
public sealed record ContractStatusChanged(
    ContractId ContractId,
    ContractStatus From,
    ContractStatus To,
    DateTimeOffset OccurredOn) : IDomainEvent;
