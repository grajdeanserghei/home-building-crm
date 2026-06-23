using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// Links a <see cref="WorkPackage"/> to a <see cref="Trade"/> it requires, <b>by id</b>. A local
/// entity owned by the work package — the persistent form of one "work package requires trade" entry
/// in the many-to-many relationship (the <c>work_package_trades</c> join table). The work package
/// holds a set of these links, never <see cref="Trade"/> objects.
/// </summary>
public sealed class WorkPackageTrade
{
    /// <summary>The trade required (referenced by id; the shared <see cref="Trade"/> vocabulary).</summary>
    public TradeId TradeId { get; private set; }

    // EF Core materialisation constructor.
    private WorkPackageTrade()
    {
    }

    // Created only by the WorkPackage root (see WorkPackage.RequireTrade/SetRequiredTrades).
    internal WorkPackageTrade(TradeId tradeId) => TradeId = tradeId;
}
