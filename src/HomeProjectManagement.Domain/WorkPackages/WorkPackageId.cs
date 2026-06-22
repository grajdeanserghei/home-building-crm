using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>Strongly-typed identity for the <see cref="WorkPackage"/> aggregate root.</summary>
public readonly record struct WorkPackageId(Guid Value) : IStronglyTypedId
{
    public static WorkPackageId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
