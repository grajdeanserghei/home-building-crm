using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core adapter for <see cref="IBillOfQuantitiesRepository"/>. Sections and their line items are
/// owned parts of the aggregate, so EF loads them with the BoQ — no explicit include is needed.
/// </summary>
public sealed class BillOfQuantitiesRepository(AppDbContext db) : IBillOfQuantitiesRepository
{
    public async Task<BillOfQuantities?> GetAsync(BoqId id, CancellationToken cancellationToken = default) =>
        await db.BillsOfQuantities.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BillOfQuantities>> ListByBidAsync(
        BidId bidId,
        CancellationToken cancellationToken = default) =>
        await db.BillsOfQuantities
            .Where(b => b.BidId == bidId)
            .ToListAsync(cancellationToken);

    public void Add(BillOfQuantities root) => db.BillsOfQuantities.Add(root);

    public void Remove(BillOfQuantities root) => db.BillsOfQuantities.Remove(root);
}
