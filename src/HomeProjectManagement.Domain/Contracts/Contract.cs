using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contracts.Events;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Contracts;

/// <summary>
/// The award for a work package: created when one bid is selected, from that bid's accepted Bill of
/// Quantities. A work package has at most one contract; the contract has its own lifecycle (status,
/// signed/started/completed dates, agreed value) that evolves after the award, and future concerns
/// (payments, invoices, progress) will reference it directly.
/// </summary>
/// <remarks>
/// Aggregate root with no internal entities. It references its <see cref="WorkPackageId"/> and the
/// <see cref="BoqId"/> of the accepted BoQ <b>by identity</b> — the bid and contractor are reached
/// through that BoQ. Construction goes through the <see cref="Award"/> factory; state changes go
/// through intention-revealing methods that enforce invariants and <c>Raise(...)</c> events. The
/// cross-aggregate award flow (selecting the bid, transitioning the work package to <c>Awarded</c>)
/// is coordinated by an application service; this aggregate is the contract's own part of it.
/// <b>No clock and no I/O in the domain</b> — callers pass <c>now</c> in.
/// </remarks>
public sealed class Contract : AggregateRoot<ContractId>
{
    /// <summary>The awarded work package (by id).</summary>
    public WorkPackageId WorkPackageId { get; private set; }

    /// <summary>The accepted BoQ this contract is based on (by id). Contractor is reached through it.</summary>
    public BoqId AcceptedBoqId { get; private set; }

    /// <summary>The contractor's/your own contract reference number. Optional.</summary>
    public string? ContractNumber { get; private set; }

    public ContractStatus Status { get; private set; }

    /// <summary>Agreed value; defaults to the accepted BoQ total but may be negotiated.</summary>
    public Money Value { get; private set; } = null!;

    /// <summary>When the contract was signed. Set on <see cref="Sign"/>; null until then.</summary>
    public DateTimeOffset? SignedOn { get; private set; }

    /// <summary>Planned/actual start of the contracted work. Optional.</summary>
    public DateTimeOffset? StartDate { get; private set; }

    /// <summary>Planned completion. Optional.</summary>
    public DateTimeOffset? PlannedEndDate { get; private set; }

    /// <summary>Actual completion. Set on <see cref="Complete"/>; null until then.</summary>
    public DateTimeOffset? ActualEndDate { get; private set; }

    /// <summary>Free-text notes. Optional.</summary>
    public string? Notes { get; private set; }

    // EF Core materialisation constructor.
    private Contract()
    {
    }

    private Contract(ContractId id, WorkPackageId workPackageId, BoqId acceptedBoqId, Money value) : base(id)
    {
        Id = id;
        WorkPackageId = workPackageId;
        AcceptedBoqId = acceptedBoqId;
        Value = value;
    }

    /// <summary>
    /// Factory: award a contract for a work package from its accepted BoQ, validating its invariants.
    /// <paramref name="now"/> is supplied by the caller (from <c>TimeProvider</c>) rather than read
    /// inside the domain. A freshly awarded contract starts <see cref="ContractStatus.Draft"/>. The
    /// "at most one contract per work package" rule spans the set of contracts and is enforced by the
    /// application service plus a unique index, not here.
    /// </summary>
    public static Contract Award(
        WorkPackageId workPackageId,
        BoqId acceptedBoqId,
        Money value,
        DateTimeOffset now,
        string? contractNumber = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? plannedEndDate = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureDatesConsistent(startDate, plannedEndDate);

        var contract = new Contract(ContractId.New(), workPackageId, acceptedBoqId, value)
        {
            Status = ContractStatus.Draft,
            ContractNumber = Trim(contractNumber),
            StartDate = startDate,
            PlannedEndDate = plannedEndDate,
            Notes = Trim(notes)
        };

        contract.Raise(new ContractAwarded(contract.Id, workPackageId, acceptedBoqId, now));
        return contract;
    }

    /// <summary>
    /// Update the contract's header details (reference number, agreed value, planned dates, notes).
    /// The awarded work package and accepted BoQ are fixed. Not allowed once the contract is closed.
    /// </summary>
    public void UpdateDetails(
        string? contractNumber,
        Money value,
        DateTimeOffset? startDate,
        DateTimeOffset? plannedEndDate,
        string? notes)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureNotClosed();
        EnsureDatesConsistent(startDate, plannedEndDate);

        ContractNumber = Trim(contractNumber);
        Value = value;
        StartDate = startDate;
        PlannedEndDate = plannedEndDate;
        Notes = Trim(notes);
    }

    /// <summary>Sign the contract, recording when it was signed.</summary>
    public void Sign(DateTimeOffset signedOn, DateTimeOffset now)
    {
        SignedOn = signedOn;
        TransitionTo(ContractStatus.Signed, now);
    }

    /// <summary>Mark the contracted work as underway.</summary>
    public void Activate(DateTimeOffset now) => TransitionTo(ContractStatus.Active, now);

    /// <summary>Complete the contract, recording the actual end date.</summary>
    public void Complete(DateTimeOffset actualEndDate, DateTimeOffset now)
    {
        ActualEndDate = actualEndDate;
        TransitionTo(ContractStatus.Completed, now);
    }

    /// <summary>Terminate the contract early.</summary>
    public void Terminate(DateTimeOffset now) => TransitionTo(ContractStatus.Terminated, now);

    private void TransitionTo(ContractStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        EnsureNotClosed();

        var previous = Status;
        Status = status;
        Raise(new ContractStatusChanged(Id, previous, status, now));
    }

    private void EnsureNotClosed()
    {
        if (Status is ContractStatus.Completed or ContractStatus.Terminated)
        {
            throw new DomainConflictException(
                $"A {Status} contract is closed and can no longer change.",
                code: "ContractClosed",
                parameters: new Dictionary<string, object?> { ["status"] = Status.ToString() });
        }
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureDatesConsistent(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is not null && end is not null && end < start)
        {
            throw new DomainValidationException("Planned end date must not be before the start date.");
        }
    }
}
