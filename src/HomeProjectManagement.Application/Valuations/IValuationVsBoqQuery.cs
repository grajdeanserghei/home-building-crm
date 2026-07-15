namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Read-only query composing the estimate-vs-real-BoQ comparison for a project's valuation catalog
/// (driving/primary port). Live — it rolls up the current BoQ subtotals every time.
/// </summary>
public interface IValuationVsBoqQuery
{
    /// <summary>
    /// The comparison under an explicit <see cref="ComparisonBasis"/>: competing BoQs of a work package
    /// collapse to the single active one, contributions sum only across work packages.
    /// </summary>
    Task<ValuationVsBoqDto?> GetAsync(Guid projectId, ComparisonBasis basis, CancellationToken cancellationToken = default);

    /// <summary>The standalone comparison on the <c>Decided</c> basis (accepted-then-selected).</summary>
    Task<ValuationVsBoqDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
