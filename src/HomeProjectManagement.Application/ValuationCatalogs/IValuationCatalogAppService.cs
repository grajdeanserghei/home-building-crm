namespace HomeProjectManagement.Application.ValuationCatalogs;

/// <summary>
/// Use-case port for the <see cref="Domain.ValuationCatalogs.ValuationCatalog"/> aggregate (driving/primary
/// port). Methods return the read model (<c>null</c> → not found) or a <c>bool</c> for delete/unlink. The
/// owning project / catalog / item id comes from the route, not the command body.
/// </summary>
public interface IValuationCatalogAppService
{
    Task<ValuationCatalogDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> CreateAsync(
        Guid projectId,
        CreateValuationCatalogCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> UpdateHeaderAsync(
        Guid id,
        UpdateValuationCatalogHeaderCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> ChangeVatRateAsync(
        Guid id,
        ChangeVatRateCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> AddItemAsync(
        Guid id,
        AddValuationItemCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> ReviseItemAsync(
        Guid id,
        Guid itemId,
        ReviseValuationItemCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> DeactivateItemAsync(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> LinkBoqSectionAsync(
        Guid id,
        Guid itemId,
        LinkBoqSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ValuationCatalogDto?> UnlinkBoqSectionAsync(
        Guid id,
        Guid itemId,
        LinkBoqSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
