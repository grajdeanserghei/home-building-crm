using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Non-generic marker for aggregate roots, so infrastructure (the unit of work) can find
/// roots in the change tracker regardless of their id type, stamp audit fields, and drain
/// their domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();

    void StampCreated(UserId by, DateTimeOffset on);

    void StampModified(UserId by, DateTimeOffset on);
}
