namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Resolves the single active ("real") BoQ per work package for a given <see cref="ComparisonBasis"/>. An
/// application-layer service over the existing repository ports — it needs no new infrastructure, so it is
/// registered in <c>AddApplication()</c> alongside the queries.
/// </summary>
public interface IRealBoqSelector
{
    /// <summary>
    /// Map of <c>workPackageId → active BoqId</c> for this basis. Work packages with nothing
    /// decided/selected are simply absent (an item mapped only to their competing BoQs is "not realized").
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> ResolveAsync(
        Guid projectId, ComparisonBasis basis, CancellationToken cancellationToken = default);
}
