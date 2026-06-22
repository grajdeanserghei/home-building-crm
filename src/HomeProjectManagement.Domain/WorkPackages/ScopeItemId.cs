using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// Local identity for a <see cref="ScopeItem"/> within the <see cref="WorkPackage"/> aggregate.
/// Unique inside its work package. A BoQ <c>Section</c> may reference it by id to allocate that
/// section's cost to this sub-scope (a deliberately loose reference validated in the application
/// service), but scope items are otherwise never managed from outside the aggregate.
/// </summary>
public readonly record struct ScopeItemId(Guid Value) : IStronglyTypedId
{
    public static ScopeItemId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
