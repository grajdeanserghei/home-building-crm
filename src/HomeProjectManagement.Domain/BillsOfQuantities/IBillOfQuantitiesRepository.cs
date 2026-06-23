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
    /// <summary>The single BoQ for a bid, or null if none has been drafted yet (at most one per bid).</summary>
    Task<BillOfQuantities?> GetByBidAsync(
        BidId bidId,
        CancellationToken cancellationToken = default);
}
