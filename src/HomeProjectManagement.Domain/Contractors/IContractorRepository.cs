using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Contractors;

/// <summary>
/// Persistence port for the <see cref="Contractor"/> aggregate (driven port; implemented by
/// EF Core in Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface IContractorRepository : IRepository<Contractor, ContractorId>
{
    /// <summary>All contractors, ordered alphabetically by name (master-data directory).</summary>
    Task<IReadOnlyList<Contractor>> ListAsync(CancellationToken cancellationToken = default);
}
