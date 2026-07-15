namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Read-only query composing a catalog's completion-progress series across its frozen snapshots
/// (driving/primary port).
/// </summary>
public interface IValuationProgressQuery
{
    Task<ValuationProgressSeriesDto?> GetSeriesAsync(Guid catalogId, CancellationToken cancellationToken = default);
}
