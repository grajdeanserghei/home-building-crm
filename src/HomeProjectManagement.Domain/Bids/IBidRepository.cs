using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// Persistence port for the <see cref="Bid"/> aggregate (driven port; implemented by EF Core in
/// Infrastructure). Lives beside the aggregate it serves. Repositories return whole bids,
/// including their discussion log.
/// </summary>
public interface IBidRepository : IRepository<Bid, BidId>
{
    /// <summary>The bids competing for one work package.</summary>
    Task<IReadOnlyList<Bid>> ListByWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default);

    /// <summary>Every bid a contractor has participated in.</summary>
    Task<IReadOnlyList<Bid>> ListByContractorAsync(
        ContractorId contractorId,
        CancellationToken cancellationToken = default);
}
