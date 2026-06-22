using HomeProjectManagement.Domain.UnitsOfMeasure;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="IUnitOfMeasureRepository"/>.</summary>
public sealed class UnitOfMeasureRepository(AppDbContext db) : IUnitOfMeasureRepository
{
    public async Task<UnitOfMeasure?> GetAsync(UnitOfMeasureId id, CancellationToken cancellationToken = default) =>
        await db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UnitOfMeasure>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = db.UnitsOfMeasure.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        return await query
            .OrderBy(u => u.Category)
            .ThenBy(u => u.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitOfMeasure?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        return await db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == normalized, cancellationToken);
    }

    public void Add(UnitOfMeasure root) => db.UnitsOfMeasure.Add(root);

    public void Remove(UnitOfMeasure root) => db.UnitsOfMeasure.Remove(root);
}
