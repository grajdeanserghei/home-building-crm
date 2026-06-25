using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.CostScenarios;

/// <summary>Strongly-typed identity for the <see cref="CostScenario"/> aggregate root.</summary>
public readonly record struct CostScenarioId(Guid Value) : IStronglyTypedId
{
    public static CostScenarioId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
