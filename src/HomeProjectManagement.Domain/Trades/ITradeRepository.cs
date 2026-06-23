using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Trades;

/// <summary>
/// Persistence port for the <see cref="Trade"/> aggregate (driven port; implemented by EF Core in
/// Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface ITradeRepository : IRepository<Trade, TradeId>
{
    /// <summary>
    /// The vocabulary, ordered by name. Inactive (retired) trades are included unless
    /// <paramref name="includeInactive"/> is false.
    /// </summary>
    Task<IReadOnlyList<Trade>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The trade with this canonical name (case-insensitive), or null. Used to enforce the
    /// unique-name invariant before defining a new trade.
    /// </summary>
    Task<Trade?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// The trades with these ids. Used by the application service to validate that a contractor's
    /// or work package's referenced trades all exist and are active before assigning them.
    /// </summary>
    Task<IReadOnlyList<Trade>> ListByIdsAsync(
        IReadOnlyCollection<TradeId> ids,
        CancellationToken cancellationToken = default);
}
