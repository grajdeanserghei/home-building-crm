namespace HomeProjectManagement.Application.Contractors;

/// <summary>
/// Driving (primary) port for contractor use cases. The minimal-API endpoints in ApiService
/// are the adapter that calls this; the host never touches EF Core or the domain directly.
/// </summary>
public interface IContractorAppService
{
    /// <summary>All contractors, ordered by name.</summary>
    Task<IReadOnlyList<ContractorDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ContractorDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContractorDto> RegisterAsync(RegisterContractorCommand command, CancellationToken cancellationToken = default);

    Task<ContractorDto?> UpdateAsync(Guid id, UpdateContractorCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
