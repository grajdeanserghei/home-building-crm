using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Contractors;

/// <summary>Strongly-typed identity for the <see cref="Contractor"/> aggregate root.</summary>
public readonly record struct ContractorId(Guid Value) : IStronglyTypedId
{
    public static ContractorId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
