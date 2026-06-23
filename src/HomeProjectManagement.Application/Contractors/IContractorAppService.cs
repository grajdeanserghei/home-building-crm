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

    /// <summary>
    /// Tag the firm with one more trade it performs (idempotent). Returns null if the contractor
    /// does not exist; an unknown or inactive trade is a validation error (HTTP 400).
    /// </summary>
    Task<ContractorDto?> AddTradeAsync(Guid id, Guid tradeId, CancellationToken cancellationToken = default);

    /// <summary>Remove one trade tag (idempotent). Returns null if the contractor does not exist.</summary>
    Task<ContractorDto?> RemoveTradeAsync(Guid id, Guid tradeId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
