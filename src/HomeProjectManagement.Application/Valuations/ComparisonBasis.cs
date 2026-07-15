namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Which real BoQ counts as "the one" per work package in the estimate-vs-real comparison. Competing BoQs
/// of a single work package are mutually-exclusive alternatives, so the read model picks one active BoQ per
/// work package (never sums them). Every basis is derived from existing aggregates each time it is
/// evaluated — there is no persisted "reference BoQ" state.
/// </summary>
public abstract record ComparisonBasis
{
    // Sealed hierarchy: only the two nested cases below (the private ctor blocks external subclasses).
    private ComparisonBasis()
    {
    }

    /// <summary>
    /// The work package's decided BoQ: the accepted contract's BoQ, else the selected bid's BoQ, else
    /// nothing realized. The default for the standalone "Estimat vs. real".
    /// </summary>
    public sealed record Decided : ComparisonBasis;

    /// <summary>A cost scenario's chosen bid per work package — the simulator's what-if.</summary>
    public sealed record Scenario(Guid CostScenarioId) : ComparisonBasis;
}
