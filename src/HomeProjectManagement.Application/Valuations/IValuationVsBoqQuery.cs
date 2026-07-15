namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Read-only query composing the estimate-vs-real-BoQ comparison for a project's valuation catalog
/// (driving/primary port). Live — it rolls up the current BoQ subtotals every time.
/// </summary>
public interface IValuationVsBoqQuery
{
    Task<ValuationVsBoqDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
