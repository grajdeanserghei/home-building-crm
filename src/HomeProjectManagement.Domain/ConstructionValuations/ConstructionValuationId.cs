using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ConstructionValuations;

/// <summary>Strongly-typed identity for the <see cref="ConstructionValuation"/> aggregate root.</summary>
public readonly record struct ConstructionValuationId(Guid Value) : IStronglyTypedId
{
    public static ConstructionValuationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
