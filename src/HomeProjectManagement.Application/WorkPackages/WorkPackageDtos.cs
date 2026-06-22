using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.WorkPackages;

/// <summary>
/// Read model returned to clients. <c>CreatedAt</c> comes from the aggregate's audit fields;
/// <c>AwardedContractId</c> is null until the package is awarded.
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
    DateTimeOffset CreatedAt);

/// <summary>
/// Input for defining a work package within a project. The owning project comes from the
/// route, not the body. A freshly defined package starts in status <c>Defined</c>.
/// </summary>
public sealed record DefineWorkPackageCommand(
    string Name,
    string? Description,
    int Sequence,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate);

/// <summary>
/// Input for editing a work package's descriptive fields and schedule.
/// </summary>
/// <remarks>
/// Status is intentionally not mutated here: work-package lifecycle transitions carry
/// invariants (e.g. award records a contract and spans the Bid/Contract aggregates), so they
/// belong on dedicated transition endpoints introduced with the award flow — not on a
/// free-form edit, unlike <c>Project</c>.
/// </remarks>
public sealed record UpdateWorkPackageCommand(
    string Name,
    string? Description,
    int Sequence,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate);
