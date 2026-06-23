using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.WorkPackages;

/// <summary>
/// Read model returned to clients. <c>CreatedAt</c> comes from the aggregate's audit fields;
/// <c>AwardedContractId</c> is null until the package is awarded. <c>ScopeItems</c> are the
/// owner-defined sub-scopes, in their intended order.
/// </summary>
public sealed record WorkPackageDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    WorkPackageStatus Status,
    int Sequence,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    Guid? AwardedContractId,
    IReadOnlyList<ScopeItemDto> ScopeItems,
    IReadOnlyCollection<Guid> RequiredTradeIds,
    DateTimeOffset CreatedAt);

/// <summary>
/// An owner-defined sub-scope of a work package (mirrors the <c>ScopeItem</c> entity). Names are
/// unique within the package; <c>Requirement</c> distinguishes a mandatory scope from one that
/// could be dropped or deferred if the budget is tight.
/// </summary>
public sealed record ScopeItemDto(
    Guid Id,
    string Name,
    string? Description,
    ScopeItemRequirement Requirement,
    int Sequence);

/// <summary>
/// Input for defining a work package within a project. The owning project comes from the
/// route, not the body. A freshly defined package starts in status <c>Defined</c>.
/// </summary>
public sealed record DefineWorkPackageCommand(
    string Name,
    string? Description,
    int Sequence,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    IReadOnlyCollection<Guid>? RequiredTradeIds);

/// <summary>
/// Input for editing a work package's descriptive fields and schedule.
/// </summary>
/// <remarks>
/// Status is intentionally not mutated here: work-package lifecycle transitions carry
/// invariants (e.g. award records a contract and spans the Bid/Contract aggregates), so they
/// belong on dedicated transition endpoints introduced with the award flow — not on a
/// free-form edit, unlike <c>Project</c>.
/// </remarks>
/// <remarks>
/// <c>RequiredTradeIds</c>, when non-null, replaces the whole set of trades the package requires
/// (each must be an existing, active trade); a <c>null</c> leaves the existing trades unchanged
/// (they are managed incrementally via the add/remove-required-trade operations).
/// </remarks>
public sealed record UpdateWorkPackageCommand(
    string Name,
    string? Description,
    int Sequence,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    IReadOnlyCollection<Guid>? RequiredTradeIds);

/// <summary>
/// Input for transitioning a work package's status. The service dispatches each target to the
/// matching intention-revealing domain method (so invariants hold — e.g. only an awarded package
/// can start). <c>Awarded</c> is reserved for the award flow and is rejected here (HTTP 409).
/// </summary>
public sealed record ChangeWorkPackageStatusCommand(WorkPackageStatus Status);

/// <summary>
/// Input for adding or editing a scope item. The name must be unique (case-insensitive) within
/// the work package; <c>Requirement</c> marks it Mandatory or Optional.
/// </summary>
public sealed record ScopeItemCommand(
    string Name,
    ScopeItemRequirement Requirement,
    string? Description,
    int Sequence);
