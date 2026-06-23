using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Budgeting;

/// <summary>
/// A read model assembled across aggregates (work packages, their bids' bills of quantities, and
/// any awarded contract) to answer one question: what is the project going to cost? Reuses
/// <see cref="MoneyDto"/> from the contracts read model. Money is reported <b>per currency</b> —
/// the <see cref="Money"/> value object refuses to sum across currencies, so there is no single
/// cross-currency grand total. Headline figures are net (VAT-exclusive).
/// </summary>
public sealed record ProjectBudgetDto(
    Guid ProjectId,
    string ProjectName,
    IReadOnlyList<WorkPackageBudgetLineDto> Lines,
    IReadOnlyList<CurrencyTotalsDto> TotalsByCurrency,
    int UnpricedWorkPackageCount);

/// <summary>How a work-package line derives its cost figure — the rule is decided server-side.</summary>
public enum BudgetLineKind
{
    /// <summary>Awarded: the figure is the contract's agreed value.</summary>
    Contract,

    /// <summary>Out to bid: the figure is the range of received BoQ totals.</summary>
    Bids,

    /// <summary>Has bids, but none carries a priced BoQ yet.</summary>
    Pending,

    /// <summary>No bids opened.</summary>
    None,
}

/// <summary>
/// One work package's cost line. <see cref="Committed"/> is set only when <see cref="Kind"/> is
/// <see cref="BudgetLineKind.Contract"/>; <see cref="Candidates"/> is populated (one entry per
/// currency) only when <see cref="Kind"/> is <see cref="BudgetLineKind.Bids"/>.
/// </summary>
public sealed record WorkPackageBudgetLineDto(
    Guid WorkPackageId,
    string Name,
    WorkPackageStatus Status,
    int Sequence,
    BudgetLineKind Kind,
    MoneyDto? Committed,
    IReadOnlyList<CandidateRangeDto> Candidates);

/// <summary>
/// The spread of candidate bid prices for one work package in a single currency. <see cref="Low"/>
/// and <see cref="High"/> are net (VAT-exclusive) BoQ totals; the <c>WithVat</c> pair is the same two
/// bids' gross totals. <see cref="BidCount"/> is how many bids in this currency carry a priced BoQ.
/// </summary>
public sealed record CandidateRangeDto(
    Currency Currency,
    MoneyDto Low,
    MoneyDto High,
    MoneyDto LowWithVat,
    MoneyDto HighWithVat,
    int BidCount);

/// <summary>
/// Project-level projection for one currency. <see cref="Committed"/> is the sum of contract values;
/// <see cref="EstimatedLow"/>/<see cref="EstimatedHigh"/> sum the candidate ranges of the work
/// packages still out to bid; the projected band adds the two (committed counts at both ends).
/// All net (VAT-exclusive).
/// </summary>
public sealed record CurrencyTotalsDto(
    Currency Currency,
    MoneyDto Committed,
    MoneyDto EstimatedLow,
    MoneyDto EstimatedHigh,
    MoneyDto ProjectedLow,
    MoneyDto ProjectedHigh);
