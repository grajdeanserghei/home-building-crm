using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// Persistence port for the <see cref="BillOfQuantities"/> aggregate (driven port; implemented by
/// EF Core in Infrastructure). Lives beside the aggregate it serves. Repositories return whole
/// bills, including their sections and line items.
/// </summary>
public interface IBillOfQuantitiesRepository : IRepository<BillOfQuantities, BoqId>
{
    /// <summary>The BoQ versions submitted within a bid, oldest version first.</summary>
    Task<IReadOnlyList<BillOfQuantities>> ListByBidAsync(
        BidId bidId,
        CancellationToken cancellationToken = default);
}
