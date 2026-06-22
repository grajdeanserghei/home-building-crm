using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>Strongly-typed identity for the <see cref="BillOfQuantities"/> aggregate root.</summary>
public readonly record struct BoqId(Guid Value) : IStronglyTypedId
{
    public static BoqId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
