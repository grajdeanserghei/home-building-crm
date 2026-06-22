using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.UnitsOfMeasure;

/// <summary>Strongly-typed identity for the <see cref="UnitOfMeasure"/> aggregate root.</summary>
public readonly record struct UnitOfMeasureId(Guid Value) : IStronglyTypedId
{
    public static UnitOfMeasureId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
