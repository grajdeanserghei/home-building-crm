using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids;

/// <summary>Strongly-typed identity for the <see cref="Bid"/> aggregate root.</summary>
public readonly record struct BidId(Guid Value) : IStronglyTypedId
{
    public static BidId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
