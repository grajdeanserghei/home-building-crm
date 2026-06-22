namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// The commit boundary port (driven). An application service opens a unit of work,
/// invokes domain behaviour through repositories, then commits once — a single
/// transaction across the aggregate roots it touched. The implementation also stamps
/// audit fields and dispatches collected domain events after the commit succeeds.
/// </summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
