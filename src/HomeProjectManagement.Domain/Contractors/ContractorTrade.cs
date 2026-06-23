using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Domain.Contractors;

/// <summary>
/// Links a <see cref="Contractor"/> to a <see cref="Trade"/> it performs, <b>by id</b>. A local
/// entity owned by the contractor — the persistent form of one "contractor performs trade" entry in
/// the many-to-many relationship (the <c>contractor_trades</c> join table). The contractor holds a
/// set of these links, never <see cref="Trade"/> objects.
/// </summary>
public sealed class ContractorTrade
{
    /// <summary>The trade performed (referenced by id; the shared <see cref="Trade"/> vocabulary).</summary>
    public TradeId TradeId { get; private set; }

    // EF Core materialisation constructor.
    private ContractorTrade()
    {
    }

    // Created only by the Contractor root (see Contractor.AssignTrade/SetTrades).
    internal ContractorTrade(TradeId tradeId) => TradeId = tradeId;
}
