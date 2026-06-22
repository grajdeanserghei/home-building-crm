namespace HomeProjectManagement.Domain.UnitsOfMeasure;

/// <summary>
/// The kind of physical quantity a unit measures. Lets the controlled vocabulary be grouped
/// and validated (e.g. a length line item should reference a <see cref="Length"/> unit).
/// Persisted as its string name. See the domain model's <c>UnitCategory</c> enum.
/// </summary>
public enum UnitCategory
{
    /// <summary>Linear measure — m, ml (linear metre), cm.</summary>
    Length,

    /// <summary>Area measure — m², mp.</summary>
    Area,

    /// <summary>Volume measure — m³, mc.</summary>
    Volume,

    /// <summary>Mass measure — kg, t (tonne), to.</summary>
    Mass,

    /// <summary>Counted units — pcs, buc.</summary>
    Count,

    /// <summary>Duration — hrs, days.</summary>
    Time,

    /// <summary>Anything that does not fit the categories above (e.g. lump-sum "set").</summary>
    Other
}
