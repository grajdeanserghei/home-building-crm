using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Trades.Events;

/// <summary>Raised when a new canonical trade is defined in the vocabulary.</summary>
public sealed record TradeDefined(
    TradeId TradeId,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent;
