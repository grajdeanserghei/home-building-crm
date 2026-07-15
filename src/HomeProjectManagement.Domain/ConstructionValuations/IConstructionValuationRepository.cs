using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Domain.ConstructionValuations;

/// <summary>
/// Persistence port for the <see cref="ConstructionValuation"/> aggregate (driven port; implemented by EF
/// Core in Infrastructure). Lives beside the aggregate it serves. Repositories return whole snapshots,
/// including their frozen items.
/// </summary>
public interface IConstructionValuationRepository
    : IRepository<ConstructionValuation, ConstructionValuationId>
{
    /// <summary>A catalog's dated snapshots, oldest first.</summary>
    Task<IReadOnlyList<ConstructionValuation>> ListByCatalogAsync(
        ValuationCatalogId valuationCatalogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The snapshot already imported from a given source document (by content hash) within a catalog, or
    /// null — backs idempotent import.
    /// </summary>
    Task<ConstructionValuation?> GetBySourceContentHashAsync(
        ValuationCatalogId valuationCatalogId,
        string sourceContentHash,
        CancellationToken cancellationToken = default);
}
