namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Generic aggregate-root repository port (driven/secondary port, implemented in
/// Infrastructure). One repository per aggregate root; repositories return and persist
/// <b>whole aggregates</b>. Internal entities (sections, line items, notes) have no
/// repository of their own.
/// </summary>
public interface IRepository<TRoot, in TId>
    where TRoot : AggregateRoot<TId>
    where TId : struct, IStronglyTypedId
{
    Task<TRoot?> GetAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TRoot root);

    void Remove(TRoot root);
}
