using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contracts;

namespace HomeProjectManagement.Application.Contracts;

/// <summary>
/// Read model returned to clients. <c>CreatedAt</c> comes from the audit fields; the work package
/// and accepted BoQ are exposed by id (the bid and contractor are reached through the BoQ).
/// </summary>
public sealed record ContractDto(
    Guid Id,
    Guid WorkPackageId,
    Guid AcceptedBoqId,
    string? ContractNumber,
    ContractStatus Status,
    MoneyDto Value,
    DateTimeOffset? SignedOn,
    DateTimeOffset? StartDate,
    DateTimeOffset? PlannedEndDate,
    DateTimeOffset? ActualEndDate,
    string? Notes,
    DateTimeOffset CreatedAt);

/// <summary>A monetary amount (mirrors the <c>Money</c> value object).</summary>
public sealed record MoneyDto(decimal Amount, Currency Currency);

/// <summary>
/// Input for awarding a contract from a chosen winning BoQ. The award is atomic: it accepts this
/// BoQ, selects its bid (rejecting the rival bids on the same work package), creates the contract,
/// and transitions the work package to <c>Awarded</c>. The work package is reached through the BoQ's
/// bid. <c>Value</c> is optional and defaults to the BoQ's total when omitted.
/// </summary>
public sealed record AwardContractCommand(
    Guid BoqId,
    MoneyDto? Value,
    string? ContractNumber,
    DateTimeOffset? StartDate,
    DateTimeOffset? PlannedEndDate,
    string? Notes);

/// <summary>Input for editing a contract's header details. The work package and accepted BoQ are fixed.</summary>
public sealed record UpdateContractCommand(
    string? ContractNumber,
    MoneyDto Value,
    DateTimeOffset? StartDate,
    DateTimeOffset? PlannedEndDate,
    string? Notes);

/// <summary>
/// Input for transitioning a contract's status (Draft → Signed → Active → Completed / Terminated).
/// <c>SignedOn</c> is required when moving to <c>Signed</c>; <c>ActualEndDate</c> when moving to
/// <c>Completed</c>.
/// </summary>
public sealed record ChangeContractStatusCommand(
    ContractStatus Status,
    DateTimeOffset? SignedOn,
    DateTimeOffset? ActualEndDate);
