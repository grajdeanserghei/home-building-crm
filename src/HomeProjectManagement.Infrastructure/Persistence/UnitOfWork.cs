using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the commit boundary. On commit it stamps audit fields on
/// added/modified roots, saves in one transaction, then dispatches the domain events those
/// roots raised — post-commit, so handlers see persisted state.
/// </summary>
public sealed class UnitOfWork(
    AppDbContext db,
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();

        var domainEvents = CollectDomainEvents();

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
            ClearDomainEvents();
        }

        return affected;
    }

    private void StampAudit()
    {
        var now = timeProvider.GetUtcNow();
        var who = currentUser.UserId;

        foreach (var entry in db.ChangeTracker.Entries<IAggregateRoot>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.StampCreated(who, now);
                    break;
                case EntityState.Modified:
                    entry.Entity.StampModified(who, now);
                    break;
            }
        }
    }

    private List<IDomainEvent> CollectDomainEvents() =>
        db.ChangeTracker.Entries<IAggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

    private void ClearDomainEvents()
    {
        foreach (var entry in db.ChangeTracker.Entries<IAggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }
    }
}
