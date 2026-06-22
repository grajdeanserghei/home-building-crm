using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.WorkPackages;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core adapter for <see cref="IBidRepository"/>. The owned discussion notes are part of the
/// aggregate, so EF loads them with the bid — no explicit include is needed.
/// </summary>
public sealed class BidRepository(AppDbContext db) : IBidRepository
{
    public async Task<Bid?> GetAsync(BidId id, CancellationToken cancellationToken = default) =>
        await db.Bids.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Bid>> ListByWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default) =>
        await db.Bids
            .Where(b => b.WorkPackageId == workPackageId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Bid>> ListByContractorAsync(
        ContractorId contractorId,
        CancellationToken cancellationToken = default) =>
        await db.Bids
            .Where(b => b.ContractorId == contractorId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsForPairAsync(
        WorkPackageId workPackageId,
        ContractorId contractorId,
        CancellationToken cancellationToken = default) =>
        await db.Bids.AnyAsync(
            b => b.WorkPackageId == workPackageId && b.ContractorId == contractorId,
            cancellationToken);

    public void Add(Bid root) => db.Bids.Add(root);

    public void Remove(Bid root) => db.Bids.Remove(root);
}
