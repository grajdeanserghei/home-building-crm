namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Base class for domain entities. Entities have identity: two entities are equal when
/// their ids are equal, regardless of their other attribute values.
/// </summary>
public abstract class Entity<TId>
    where TId : struct, IStronglyTypedId
{
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    // Parameterless ctor for EF Core materialisation only.
    protected Entity()
    {
    }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && other.GetType() == GetType() && other.Id.Equals(Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
