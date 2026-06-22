namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// A fact that happened in the domain (e.g. <c>ProjectCreated</c>). Raised by aggregate
/// roots, collected on <see cref="AggregateRoot{TId}"/>, and dispatched after commit by
/// the unit of work via the <c>IDomainEventDispatcher</c> port (Application layer).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
