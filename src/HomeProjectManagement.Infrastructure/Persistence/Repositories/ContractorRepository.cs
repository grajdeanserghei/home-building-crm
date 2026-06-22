using HomeProjectManagement.Domain.Contractors;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="IContractorRepository"/>.</summary>
public sealed class ContractorRepository(AppDbContext db) : IContractorRepository
{
    public async Task<Contractor?> GetAsync(ContractorId id, CancellationToken cancellationToken = default) =>
        await db.Contractors.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Contractor>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Contractors.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public void Add(Contractor root) => db.Contractors.Add(root);

    public void Remove(Contractor root) => db.Contractors.Remove(root);
}
