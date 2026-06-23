using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Trades;

/// <summary>Strongly-typed identity for the <see cref="Trade"/> aggregate root.</summary>
public readonly record struct TradeId(Guid Value) : IStronglyTypedId
{
    public static TradeId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
