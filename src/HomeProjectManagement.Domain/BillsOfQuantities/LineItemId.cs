using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>Local identity for a <see cref="LineItem"/> within the Bill of Quantities aggregate.</summary>
public readonly record struct LineItemId(Guid Value) : IStronglyTypedId
{
    public static LineItemId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
