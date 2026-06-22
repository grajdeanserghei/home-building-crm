namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// Whether a <see cref="ScopeItem"/> must be done or could be dropped if money is tight —
/// the driver behind owner-defined sub-scopes. Persisted as its string name. See the domain
/// model's <c>ScopeItemRequirement</c> enum.
/// </summary>
public enum ScopeItemRequirement
{
    /// <summary>Must be done; not a candidate for cost-cutting.</summary>
    Mandatory,

    /// <summary>Could be dropped or deferred if the budget is tight.</summary>
    Optional
}
