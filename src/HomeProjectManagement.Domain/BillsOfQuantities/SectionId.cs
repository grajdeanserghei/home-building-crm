using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>Local identity for a <see cref="Section"/> within the Bill of Quantities aggregate.</summary>
public readonly record struct SectionId(Guid Value) : IStronglyTypedId
{
    public static SectionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
