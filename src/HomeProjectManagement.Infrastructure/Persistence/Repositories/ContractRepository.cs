using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.WorkPackages;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core adapter for <see cref="IContractRepository"/>. The contract is a flat aggregate (no owned
/// child collections), so it loads directly.
/// </summary>
public sealed class ContractRepository(AppDbContext db) : IContractRepository
{
    public async Task<Contract?> GetAsync(ContractId id, CancellationToken cancellationToken = default) =>
        await db.Contracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Contract>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Contracts
            .OrderByDescending(c => c.CreatedOn)
            .ToListAsync(cancellationToken);

    public async Task<Contract?> GetByWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default) =>
        await db.Contracts.FirstOrDefaultAsync(c => c.WorkPackageId == workPackageId, cancellationToken);

    public async Task<bool> ExistsForWorkPackageAsync(
        WorkPackageId workPackageId,
        CancellationToken cancellationToken = default) =>
        await db.Contracts.AnyAsync(c => c.WorkPackageId == workPackageId, cancellationToken);

    public void Add(Contract root) => db.Contracts.Add(root);

    public void Remove(Contract root) => db.Contracts.Remove(root);
}
