using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Application.Abstractions;

/// <summary>
/// Driven port that dispatches domain events collected on aggregate roots. Invoked by the
/// unit of work <b>after</b> a successful commit. Implemented in Infrastructure by an
/// in-process handler registry.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
