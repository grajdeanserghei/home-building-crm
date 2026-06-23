using HomeProjectManagement.Domain.Trades;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>EF Core adapter for <see cref="ITradeRepository"/>.</summary>
public sealed class TradeRepository(AppDbContext db) : ITradeRepository
{
    public async Task<Trade?> GetAsync(TradeId id, CancellationToken cancellationToken = default) =>
        await db.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Trade>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = db.Trades.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<Trade?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return await db.Trades.FirstOrDefaultAsync(
            t => t.Name.ToLower() == normalized.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Trade>> ListByIdsAsync(
        IReadOnlyCollection<TradeId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idList = ids.ToList();
        return await db.Trades.Where(t => idList.Contains(t.Id)).ToListAsync(cancellationToken);
    }

    public void Add(Trade root) => db.Trades.Add(root);

    public void Remove(Trade root) => db.Trades.Remove(root);
}
