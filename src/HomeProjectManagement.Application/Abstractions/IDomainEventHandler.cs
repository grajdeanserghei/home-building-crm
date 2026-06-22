using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Application.Abstractions;

/// <summary>
/// Handles a domain event after commit. Implementations are registered in DI and resolved
/// by the <see cref="IDomainEventDispatcher"/>. None exist yet — this is the seam where
/// post-commit reactions (e.g. on <c>WorkPackageAwarded</c>) will plug in.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
