namespace HomeProjectManagement.Application.ConstructionValuations;

/// <summary>
/// Use-case port for the <see cref="Domain.ConstructionValuations.ConstructionValuation"/> aggregate
/// (driving/primary port). Capture is idempotent by source content hash. The owning catalog id comes from
/// the route, not the command body.
/// </summary>
public interface IConstructionValuationAppService
{
    Task<ConstructionValuationDto?> CaptureAsync(
        Guid catalogId,
        CaptureConstructionValuationCommand command,
        CancellationToken cancellationToken = default);

    Task<ConstructionValuationDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConstructionValuationDto>?> ListByCatalogAsync(
        Guid catalogId,
        CancellationToken cancellationToken = default);
}
