namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// What a bill of quantities is priced against, as the supplier provided it: the building as a whole,
/// or a single apartment. For a <see cref="PerApartment"/> quote, the cost for the whole build is the
/// quote's total multiplied by the project's apartment-unit count. Persisted as its string name
/// (matches the frontend's union type).
/// </summary>
public enum BudgetScopeKind
{
    /// <summary>The quote covers the entire building; its total counts once (multiplier 1).</summary>
    EntireBuilding,

    /// <summary>The quote is the price for one apartment; its total counts once per apartment unit.</summary>
    PerApartment
}
