using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HomeProjectManagement.Infrastructure.Events;

/// <summary>
/// In-process <see cref="IDomainEventDispatcher"/>: for each event it resolves every
/// registered <see cref="IDomainEventHandler{TEvent}"/> from DI and invokes it. No handlers
/// are registered yet — this is the wired-up seam for post-commit reactions.
/// </summary>
public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
