using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Contractors.Events;

/// <summary>Raised when a new contractor firm is registered as master data.</summary>
public sealed record ContractorRegistered(
    ContractorId ContractorId,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent;
