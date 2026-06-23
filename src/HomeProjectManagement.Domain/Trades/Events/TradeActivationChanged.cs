using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Trades.Events;

/// <summary>
/// Raised when a trade is retired (deactivated) or brought back into use (activated). Retirement
/// is preferred over deletion because contractors and work packages may still reference the trade.
/// </summary>
public sealed record TradeActivationChanged(
    TradeId TradeId,
    bool IsActive,
    DateTimeOffset OccurredOn) : IDomainEvent;
