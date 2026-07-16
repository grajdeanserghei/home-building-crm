using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.CostScenarios;

// ----- Commands (write input) -----

/// <summary>Input for creating a cost scenario within a project.</summary>
public sealed record CreateCostScenarioCommand(string Name, string? Description);

/// <summary>Input for editing a scenario's header details.</summary>
public sealed record UpdateCostScenarioCommand(string Name, string? Description);

/// <summary>
/// Input for choosing a bid for a work package within a scenario (upsert — replaces any existing
/// choice for that work package).
/// </summary>
public sealed record IncludeBidCommand(Guid WorkPackageId, Guid BidId);

// ----- Summaries (list read model) -----

/// <summary>One row of a project's scenario list.</summary>
public sealed record CostScenarioSummaryDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    int WorkPackageCount,
    DateTimeOffset CreatedAt);

// ----- Computed result (the simulator view) -----

/// <summary>
/// A scenario's computed cost picture: a line per included bill of quantities plus per-currency
/// totals and an approximate EUR-equivalent. Money is reported <b>per currency</b> — the
/// <see cref="Money"/> value object refuses to sum across currencies — and both net (VAT-exclusive)
/// and gross (VAT-inclusive) figures are given. Figures are scaled to the whole build: a BoQ priced
/// <i>per apartment</i> is multiplied by the project's apartment-unit count. <see cref="ApartmentUnits"/>
/// (the project's dwelling-unit count) lets a client derive each apartment's share: a per-apartment
/// figure is a whole-build figure divided by <see cref="ApartmentUnits"/>.
/// </summary>
public sealed record CostScenarioResultDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    int ApartmentUnits,
    string? Description,
    IReadOnlyList<ScenarioLineDto> Lines,
    IReadOnlyList<ScenarioCurrencyTotalDto> TotalsByCurrency,
    ScenarioEurEquivalentDto? EurEquivalent,
    decimal RonPerEur,
    DateTimeOffset CreatedAt);

/// <summary>
/// One included work package's cost line. <see cref="Priced"/> is false when the chosen bid's BoQ is
/// missing, rejected/withdrawn, or has a zero total — such a line is shown but contributes nothing to
/// the totals (and its money fields are zero in the BoQ's would-be currency / RON).
/// </summary>
public sealed record ScenarioLineDto(
    Guid WorkPackageId,
    string WorkPackageName,
    int Sequence,
    Guid BidId,
    Guid ContractorId,
    string ContractorName,
    Guid? BoqId,
    BudgetScopeKind Scope,
    int Multiplier,
    MoneyDto Net,
    MoneyDto Gross,
    MoneyDto EurEquivalentGross,
    bool Priced);

/// <summary>The scenario's net and gross totals for one currency (single values — the selection is deterministic).</summary>
public sealed record ScenarioCurrencyTotalDto(
    Currency Currency,
    MoneyDto Net,
    MoneyDto Gross);

/// <summary>
/// The per-currency totals converted to EUR and summed into one comparable figure. Approximate —
/// computed from a single app-wide display rate (<see cref="RonPerEur"/>, "1 EUR = N RON"), not the
/// per-BoQ pinned rates. Null when there is nothing to convert.
/// </summary>
public sealed record ScenarioEurEquivalentDto(
    decimal RonPerEur,
    MoneyDto Net,
    MoneyDto Gross);

// ----- Candidates (editor data) -----

/// <summary>The priced bids available to choose from for one work package.</summary>
public sealed record ScenarioCandidateWorkPackageDto(
    Guid WorkPackageId,
    string Name,
    int Sequence,
    IReadOnlyList<ScenarioCandidateBidDto> Bids);

/// <summary>
/// One selectable bid for a work package: a contractor whose current BoQ carries a price. Net/gross
/// are the effective (whole-build) totals so per-apartment and entire-building quotes compare on equal terms.
/// </summary>
public sealed record ScenarioCandidateBidDto(
    Guid BidId,
    Guid ContractorId,
    string ContractorName,
    string? Label,
    Guid BoqId,
    BudgetScopeKind Scope,
    MoneyDto Net,
    MoneyDto Gross);
