using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Contracts;

/// <summary>
/// Thin orchestration over the <see cref="Contract"/> aggregate. Awarding is the single atomic
/// step that completes the selection process across four aggregates in one commit: it accepts the
/// chosen winning BoQ, selects its bid (rejecting the rival bids on the same work package), creates
/// the contract from that BoQ — defaulting the value to its total — and transitions the work package
/// to <c>Awarded</c>. The cross-instance rules the single aggregates cannot see ("one contract per
/// work package", "at most one Selected bid per work package") are enforced here and backed by unique
/// indexes. Audit fields are stamped inside the unit of work.
/// </summary>
public sealed class ContractAppService(
    IContractRepository repository,
    IBillOfQuantitiesRepository boqs,
    IBidRepository bids,
    IWorkPackageRepository workPackages,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IContractAppService
{
    public async Task<IReadOnlyList<ContractDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var contracts = await repository.ListAsync(cancellationToken);
        return contracts.Select(ToDto).ToList();
    }

    public async Task<ContractDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetAsync(new ContractId(id), cancellationToken);
        return contract is null ? null : ToDto(contract);
    }

    public async Task<ContractDto?> GetByWorkPackageAsync(Guid workPackageId, CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetByWorkPackageAsync(new WorkPackageId(workPackageId), cancellationToken);
        return contract is null ? null : ToDto(contract);
    }

    public async Task<ContractDto?> AwardAsync(
        AwardContractCommand command,
        CancellationToken cancellationToken = default)
    {
        // The chosen winning BoQ is the thing a contract is created from; it must exist.
        var boq = await boqs.GetAsync(new BoqId(command.BoqId), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        // Its bid — and through it the work package — must still exist.
        var bid = await bids.GetAsync(boq.BidId, cancellationToken);
        if (bid is null)
        {
            throw new DomainConflictException("The BoQ's bid no longer exists.");
        }

        var workPackage = await workPackages.GetAsync(bid.WorkPackageId, cancellationToken);
        if (workPackage is null)
        {
            throw new DomainConflictException("The bid's work package no longer exists.");
        }

        // One contract per work package. The unique index is the backstop; this check turns the
        // common case into a clear conflict rather than a DB exception.
        if (await repository.ExistsForWorkPackageAsync(workPackage.Id, cancellationToken))
        {
            throw new DomainConflictException("This work package has already been awarded a contract.");
        }

        var now = timeProvider.GetUtcNow();

        // The full award flow, atomically in one commit:
        // 1. accept the winning BoQ (a closed/terminal BoQ rejects this with a 409),
        boq.Accept(now);

        // 2. select its bid, rejecting the other live bids on the same work package so that at most
        //    one bid per work package is ever Selected,
        var siblings = await bids.ListByWorkPackageAsync(bid.WorkPackageId, cancellationToken);
        foreach (var other in siblings)
        {
            if (other.Id != bid.Id && other.Status is not (BidStatus.Withdrawn or BidStatus.Rejected))
            {
                other.Reject(now);
            }
        }

        bid.Select(now);

        // 3. create the contract from the now-accepted BoQ (value defaults to the BoQ total),
        var value = command.Value is null ? boq.Total : ToMoney(command.Value);

        var contract = Contract.Award(
            workPackage.Id,
            boq.Id,
            value,
            now,
            command.ContractNumber,
            command.StartDate,
            command.PlannedEndDate,
            command.Notes);

        // 4. transition the work package to Awarded, recording the contract.
        workPackage.Award(contract.Id, now);

        repository.Add(contract);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<ContractDto?> UpdateAsync(
        Guid id,
        UpdateContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetAsync(new ContractId(id), cancellationToken);
        if (contract is null)
        {
            return null;
        }

        contract.UpdateDetails(
            command.ContractNumber,
            ToMoney(command.Value),
            command.StartDate,
            command.PlannedEndDate,
            command.Notes);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<ContractDto?> ChangeStatusAsync(
        Guid id,
        ChangeContractStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetAsync(new ContractId(id), cancellationToken);
        if (contract is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        switch (command.Status)
        {
            case ContractStatus.Signed:
                if (command.SignedOn is null)
                {
                    throw new DomainValidationException("A signed date is required to sign a contract.");
                }

                contract.Sign(command.SignedOn.Value, now);
                break;

            case ContractStatus.Active:
                contract.Activate(now);
                break;

            case ContractStatus.Completed:
                if (command.ActualEndDate is null)
                {
                    throw new DomainValidationException("An actual end date is required to complete a contract.");
                }

                contract.Complete(command.ActualEndDate.Value, now);
                break;

            case ContractStatus.Terminated:
                contract.Terminate(now);
                break;

            default:
                throw new DomainConflictException("A contract cannot be returned to Draft.");
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetAsync(new ContractId(id), cancellationToken);
        if (contract is null)
        {
            return false;
        }

        repository.Remove(contract);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static Money ToMoney(MoneyDto dto) => new(dto.Amount, dto.Currency);

    private static ContractDto ToDto(Contract contract) => new(
        contract.Id.Value,
        contract.WorkPackageId.Value,
        contract.AcceptedBoqId.Value,
        contract.ContractNumber,
        contract.Status,
        new MoneyDto(contract.Value.Amount, contract.Value.Currency),
        contract.SignedOn,
        contract.StartDate,
        contract.PlannedEndDate,
        contract.ActualEndDate,
        contract.Notes,
        contract.CreatedOn);
}
