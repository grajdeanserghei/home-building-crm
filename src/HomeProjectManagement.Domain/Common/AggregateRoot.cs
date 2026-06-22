using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Base class for aggregate roots — the only entities referenced from outside their
/// aggregate (always by id). Roots are the consistency and transaction boundary: they
/// collect <see cref="IDomainEvent"/>s and carry the audit fields stamped on commit.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IStronglyTypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>Events raised but not yet dispatched. Cleared post-commit by the unit of work.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // ----- Audit fields (stamped by the unit of work from ICurrentUser + TimeProvider) -----
    public DateTimeOffset CreatedOn { get; private set; }
    public UserId CreatedBy { get; private set; }
    public DateTimeOffset ModifiedOn { get; private set; }
    public UserId ModifiedBy { get; private set; }

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Stamp creation audit. Called by the unit of work for newly added roots.</summary>
    public void StampCreated(UserId by, DateTimeOffset on)
    {
        CreatedBy = by;
        CreatedOn = on;
        ModifiedBy = by;
        ModifiedOn = on;
    }

    /// <summary>Stamp modification audit. Called by the unit of work for changed roots.</summary>
    public void StampModified(UserId by, DateTimeOffset on)
    {
        ModifiedBy = by;
        ModifiedOn = on;
    }
}
