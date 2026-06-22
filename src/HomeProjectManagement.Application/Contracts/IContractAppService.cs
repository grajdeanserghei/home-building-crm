namespace HomeProjectManagement.Application.Contracts;

/// <summary>
/// Driving (primary) port for contract use cases — the award of a work package and the contract's
/// lifecycle thereafter. The minimal-API endpoints in ApiService are the adapter that calls this;
/// the host never touches EF Core or the domain directly. Methods report invariant violations
/// (e.g. awarding from a non-accepted BoQ, a work package already under contract, an illegal status
/// transition) as <see cref="InvalidOperationException"/>, which the endpoints map to HTTP 409.
/// </summary>
public interface IContractAppService
{
    /// <summary>Every awarded contract.</summary>
    Task<IReadOnlyList<ContractDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ContractDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The contract awarded for a work package, or null if it has none yet.</summary>
    Task<ContractDto?> GetByWorkPackageAsync(Guid workPackageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Award a contract from an accepted BoQ, transitioning its work package to <c>Awarded</c>.
    /// Returns null if the accepted BoQ does not exist. Throws <see cref="InvalidOperationException"/>
    /// if the BoQ is not accepted, its bid is not selected, or the work package is already under
    /// contract.
    /// </summary>
    Task<ContractDto?> AwardAsync(AwardContractCommand command, CancellationToken cancellationToken = default);

    Task<ContractDto?> UpdateAsync(Guid id, UpdateContractCommand command, CancellationToken cancellationToken = default);

    /// <summary>Transition the contract's status. Returns null if it does not exist.</summary>
    Task<ContractDto?> ChangeStatusAsync(Guid id, ChangeContractStatusCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
