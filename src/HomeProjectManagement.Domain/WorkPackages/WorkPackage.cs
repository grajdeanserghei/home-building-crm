using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages.Events;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// A defined scope of work within a project that is procured as a unit (e.g. La Roșu,
/// Tâmplărie). Defined up front, before any contractor exists; contractors are then found
/// per work package via bids, and one bid is eventually awarded a contract.
/// </summary>
/// <remarks>
/// Aggregate root. It references its owning <see cref="ProjectId"/> and, once awarded, a
/// <see cref="ContractId"/> — both <b>by identity</b>, never by holding the other aggregate.
/// State changes go through intention-revealing methods that enforce invariants; construction
/// goes through the <see cref="Define"/> factory.
/// </remarks>
public sealed class WorkPackage : AggregateRoot<WorkPackageId>
{
    private readonly List<ScopeItem> _scopeItems = [];

    /// <summary>The owning project (by id).</summary>
    public ProjectId ProjectId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkPackageStatus Status { get; private set; }

    /// <summary>Display/intended order within the project (La Roșu before Tâmplărie …).</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional planned start of this package's work.</summary>
    public DateTimeOffset? PlannedStartDate { get; private set; }

    /// <summary>Optional planned completion of this package.</summary>
    public DateTimeOffset? PlannedEndDate { get; private set; }

    /// <summary>
    /// The awarded contract (by id). Null until awarded; set by <see cref="Award"/>. Its
    /// presence is the invariant counterpart of <see cref="WorkPackageStatus.Awarded"/>.
    /// </summary>
    public ContractId? AwardedContractId { get; private set; }

    /// <summary>
    /// The owner-defined sub-scopes (internal entities, ordered by <see cref="ScopeItem.Sequence"/>).
    /// Mutated only through <see cref="AddScopeItem"/>, <see cref="UpdateScopeItem"/> and
    /// <see cref="RemoveScopeItem"/>; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<ScopeItem> ScopeItems => _scopeItems.AsReadOnly();

    // EF Core materialisation constructor.
    private WorkPackage()
    {
    }

    private WorkPackage(WorkPackageId id, ProjectId projectId, string name) : base(id)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
    }

    /// <summary>
    /// Factory: define a new work package within a project, validating its invariants.
    /// <paramref name="now"/> is supplied by the caller (from <c>TimeProvider</c>) rather
    /// than read inside the domain. A freshly defined package starts <see cref="WorkPackageStatus.Defined"/>.
    /// </summary>
    public static WorkPackage Define(
        ProjectId projectId,
        string name,
        DateTimeOffset now,
        string? description = null,
        int sequence = 1,
        DateTimeOffset? plannedStartDate = null,
        DateTimeOffset? plannedEndDate = null)
    {
        EnsureSequenceValid(sequence);

        var workPackage = new WorkPackage(WorkPackageId.New(), projectId, NormalizeName(name))
        {
            Description = Trim(description),
            Status = WorkPackageStatus.Defined,
            Sequence = sequence,
            PlannedStartDate = plannedStartDate,
            PlannedEndDate = plannedEndDate
        };

        EnsureDatesConsistent(workPackage.PlannedStartDate, workPackage.PlannedEndDate);
        workPackage.Raise(new WorkPackageDefined(workPackage.Id, projectId, workPackage.Name, now));
        return workPackage;
    }

    /// <summary>Rename the work package.</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Update the free-text scope notes.</summary>
    public void Describe(string? description) => Description = Trim(description);

    /// <summary>Change the display/intended order within the project (1-based).</summary>
    public void Reorder(int sequence)
    {
        EnsureSequenceValid(sequence);
        Sequence = sequence;
    }

    /// <summary>Set or clear the planned start and end dates.</summary>
    public void Reschedule(DateTimeOffset? plannedStartDate, DateTimeOffset? plannedEndDate)
    {
        EnsureDatesConsistent(plannedStartDate, plannedEndDate);
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
    }

    /// <summary>
    /// Add an owner-defined sub-scope and return it, enforcing the "name unique within the work
    /// package" invariant (case-insensitive). <paramref name="now"/> is supplied by the caller and
    /// stamps the raised event.
    /// </summary>
    public ScopeItem AddScopeItem(
        string name,
        ScopeItemRequirement requirement,
        DateTimeOffset now,
        string? description = null,
        int sequence = 0)
    {
        var normalized = ScopeItem.NormalizeName(name);
        EnsureScopeItemNameUnique(normalized, null);

        var scopeItem = new ScopeItem(ScopeItemId.New(), normalized, requirement, sequence, description);
        _scopeItems.Add(scopeItem);
        Raise(new ScopeItemAdded(Id, scopeItem.Id, scopeItem.Name, requirement, now));
        return scopeItem;
    }

    /// <summary>
    /// Update an existing sub-scope, keeping the unique-name invariant (the item itself excluded
    /// from the check). Returns false if no scope item with that id exists.
    /// </summary>
    public bool UpdateScopeItem(
        ScopeItemId scopeItemId,
        string name,
        ScopeItemRequirement requirement,
        string? description = null,
        int sequence = 0)
    {
        var scopeItem = _scopeItems.FirstOrDefault(si => si.Id == scopeItemId);
        if (scopeItem is null)
        {
            return false;
        }

        var normalized = ScopeItem.NormalizeName(name);
        EnsureScopeItemNameUnique(normalized, scopeItemId);

        scopeItem.Update(normalized, requirement, sequence, description);
        return true;
    }

    /// <summary>Remove a sub-scope. Returns false if no scope item with that id exists.</summary>
    public bool RemoveScopeItem(ScopeItemId scopeItemId)
    {
        var scopeItem = _scopeItems.FirstOrDefault(si => si.Id == scopeItemId);
        if (scopeItem is null)
        {
            return false;
        }

        _scopeItems.Remove(scopeItem);
        return true;
    }

    private void EnsureScopeItemNameUnique(string name, ScopeItemId? excluding)
    {
        var clashes = _scopeItems.Any(si =>
            si.Id != excluding && string.Equals(si.Name, name, StringComparison.OrdinalIgnoreCase));
        if (clashes)
        {
            throw new DomainConflictException(
                $"A scope item named '{name}' already exists in this work package.");
        }
    }

    /// <summary>
    /// Transition to a new status, raising an event if it changed. Reaching
    /// <see cref="WorkPackageStatus.Awarded"/> is not allowed here — it carries the
    /// "a contract is set" invariant and must go through <see cref="Award"/>.
    /// </summary>
    public void ChangeStatus(WorkPackageStatus status, DateTimeOffset now)
    {
        if (status == WorkPackageStatus.Awarded)
        {
            throw new DomainConflictException(
                "A work package becomes Awarded only via Award, which records the contract.");
        }

        TransitionTo(status, now);
    }

    /// <summary>Open the package for bidding.</summary>
    public void OpenForBids(DateTimeOffset now) => TransitionTo(WorkPackageStatus.OpenForBids, now);

    /// <summary>
    /// Award the package to a selected bid's contract: records the contract id and moves to
    /// <see cref="WorkPackageStatus.Awarded"/>. The cross-aggregate award flow (selecting the
    /// bid, creating the contract) is coordinated by an application service; this method is the
    /// work package's part of it. A cancelled package cannot be awarded.
    /// </summary>
    public void Award(ContractId contractId, DateTimeOffset now)
    {
        if (Status == WorkPackageStatus.Cancelled)
        {
            throw new DomainConflictException("A cancelled work package cannot be awarded.");
        }

        AwardedContractId = contractId;
        TransitionTo(WorkPackageStatus.Awarded, now);
        Raise(new WorkPackageAwarded(Id, contractId, now));
    }

    /// <summary>Mark the awarded work as underway.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != WorkPackageStatus.Awarded)
        {
            throw new DomainConflictException("Only an awarded work package can be started.");
        }

        TransitionTo(WorkPackageStatus.InProgress, now);
    }

    /// <summary>Mark the work as completed.</summary>
    public void Complete(DateTimeOffset now) => TransitionTo(WorkPackageStatus.Completed, now);

    /// <summary>Cancel the work package.</summary>
    public void Cancel(DateTimeOffset now) => TransitionTo(WorkPackageStatus.Cancelled, now);

    private void TransitionTo(WorkPackageStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        var previous = Status;
        Status = status;
        Raise(new WorkPackageStatusChanged(Id, previous, status, now));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Work package name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureDatesConsistent(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is not null && end is not null && end < start)
        {
            throw new DomainValidationException("Planned end date must not be before the planned start date.");
        }
    }

    private static void EnsureSequenceValid(int sequence)
    {
        if (sequence < 1)
        {
            throw new DomainValidationException("Work package order must be 1 or greater.", nameof(sequence));
        }
    }
}
