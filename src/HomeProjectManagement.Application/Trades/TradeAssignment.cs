using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Application.Trades;

/// <summary>
/// Shared guard used by the Contractor and Work Package use cases when (re)assigning trades:
/// validates the requested trade ids against the shared <see cref="Trade"/> vocabulary so a root
/// only ever references existing, active trades (the invariant "each must point at an existing,
/// validated Trade"). A failed validation surfaces as a domain validation error (HTTP 400).
/// </summary>
internal static class TradeAssignment
{
    /// <summary>
    /// Resolve raw trade-id guids to a distinct set of <see cref="TradeId"/>, verifying each one
    /// exists and is active. An empty or null input yields an empty set (a root may reference no
    /// trades). Throws <see cref="DomainValidationException"/> if any id is unknown or inactive.
    /// </summary>
    public static async Task<IReadOnlyList<TradeId>> ResolveAsync(
        ITradeRepository trades,
        IReadOnlyCollection<Guid>? tradeIds,
        CancellationToken cancellationToken)
    {
        if (tradeIds is null || tradeIds.Count == 0)
        {
            return [];
        }

        var distinct = tradeIds.Distinct().Select(id => new TradeId(id)).ToList();
        var found = await trades.ListByIdsAsync(distinct, cancellationToken);

        if (found.Count != distinct.Count)
        {
            throw new DomainValidationException(
                "One or more of the selected trades do not exist.", "tradeIds");
        }

        if (found.Any(t => !t.IsActive))
        {
            throw new DomainValidationException(
                "One or more of the selected trades are retired (inactive).", "tradeIds");
        }

        return distinct;
    }
}
