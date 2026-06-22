using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Contracts;

/// <summary>
/// Persistence port for the <see cref="Contract"/> aggregate (driven port; implemented by EF Core
/// in Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface IContractRepository : IRepository<Contract, ContractId>
{
    /// <summary>Every contract, most recently awarded first.</summary>
    Task<IReadOnlyList<Contract>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>The contract awarded for a work package, or null if it has none yet.</summary>
    Task<Contract?> GetByWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default);

    /// <summary>Whether a contract has already been awarded for the work package.</summary>
    Task<bool> ExistsForWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default);
}
