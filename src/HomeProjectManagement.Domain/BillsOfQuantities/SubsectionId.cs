using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>Local identity for a <see cref="Subsection"/> within the Bill of Quantities aggregate.</summary>
public readonly record struct SubsectionId(Guid Value) : IStronglyTypedId
{
    public static SubsectionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
