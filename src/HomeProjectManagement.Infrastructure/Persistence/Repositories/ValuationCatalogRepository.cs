using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core adapter for <see cref="IValuationCatalogRepository"/>. Items and their links are owned parts of
/// the aggregate, so EF loads them with the catalog — no explicit include is needed.
/// </summary>
public sealed class ValuationCatalogRepository(AppDbContext db) : IValuationCatalogRepository
{
    public async Task<ValuationCatalog?> GetAsync(ValuationCatalogId id, CancellationToken cancellationToken = default) =>
        await db.ValuationCatalogs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<ValuationCatalog?> GetByProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default) =>
        await db.ValuationCatalogs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

    public void Add(ValuationCatalog root) => db.ValuationCatalogs.Add(root);

    public void Remove(ValuationCatalog root) => db.ValuationCatalogs.Remove(root);
}
