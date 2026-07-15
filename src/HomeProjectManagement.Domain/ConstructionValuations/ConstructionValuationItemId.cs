using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ConstructionValuations;

/// <summary>
/// Strongly-typed local identity for a <see cref="ConstructionValuationItem"/> within the
/// <see cref="ConstructionValuation"/> aggregate.
/// </summary>
public readonly record struct ConstructionValuationItemId(Guid Value) : IStronglyTypedId
{
    public static ConstructionValuationItemId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
