using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// Persistence port for the <see cref="ValuationCatalog"/> aggregate (driven port; implemented by EF
/// Core in Infrastructure). Lives beside the aggregate it serves. Repositories return whole catalogs,
/// including their items and links.
/// </summary>
public interface IValuationCatalogRepository : IRepository<ValuationCatalog, ValuationCatalogId>
{
    /// <summary>The project's single valuation catalog, or null if none exists yet.</summary>
    Task<ValuationCatalog?> GetByProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default);
}
