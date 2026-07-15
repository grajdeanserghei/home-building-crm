using HomeProjectManagement.Domain.ConstructionValuations;
using HomeProjectManagement.Domain.ValuationCatalogs;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core adapter for <see cref="IConstructionValuationRepository"/>. The frozen assessed items are owned
/// parts of the aggregate, so EF loads them with the snapshot — no explicit include is needed.
/// </summary>
public sealed class ConstructionValuationRepository(AppDbContext db) : IConstructionValuationRepository
{
    public async Task<ConstructionValuation?> GetAsync(ConstructionValuationId id, CancellationToken cancellationToken = default) =>
        await db.ConstructionValuations.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ConstructionValuation>> ListByCatalogAsync(
        ValuationCatalogId valuationCatalogId,
        CancellationToken cancellationToken = default) =>
        await db.ConstructionValuations
            .Where(v => v.ValuationCatalogId == valuationCatalogId)
            .OrderBy(v => v.AssessedOn)
            .ToListAsync(cancellationToken);

    public async Task<ConstructionValuation?> GetBySourceContentHashAsync(
        ValuationCatalogId valuationCatalogId,
        string sourceContentHash,
        CancellationToken cancellationToken = default) =>
        await db.ConstructionValuations.FirstOrDefaultAsync(
            v => v.ValuationCatalogId == valuationCatalogId && v.SourceContentHash == sourceContentHash,
            cancellationToken);

    public void Add(ConstructionValuation root) => db.ConstructionValuations.Add(root);

    public void Remove(ConstructionValuation root) => db.ConstructionValuations.Remove(root);
}
