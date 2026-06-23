namespace HomeProjectManagement.Application.Trades;

/// <summary>
/// Driving (primary) port for trade use cases. The minimal-API endpoints in ApiService are the
/// adapter that calls this; the host never touches EF Core or the domain directly.
/// </summary>
public interface ITradeAppService
{
    /// <summary>The vocabulary, ordered by name; retired trades included by default.</summary>
    Task<IReadOnlyList<TradeDto>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    Task<TradeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Define a trade. Reports a conflict if the canonical name is already taken.</summary>
    Task<DefineTradeResult> DefineAsync(
        DefineTradeCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Edit a trade's name and code. Returns null if it does not exist.</summary>
    Task<TradeDto?> UpdateAsync(
        Guid id,
        UpdateTradeCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Retire or reinstate a trade. Returns false if it does not exist.</summary>
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
