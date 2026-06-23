using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Budgeting;

/// <summary>
/// Composes <see cref="ProjectBudgetDto"/> from the existing repository ports (read-only). For each
/// work package the figure is chosen in order: an awarded contract's value wins; otherwise the range
/// of its bids' current priced BoQ totals; otherwise it is pending (bids, no price) or has no bids.
/// Totals are accumulated per currency because <see cref="Money"/> rejects cross-currency arithmetic.
/// </summary>
public sealed class ProjectBudgetQuery(
    IProjectRepository projects,
    IWorkPackageRepository workPackages,
    IBidRepository bids,
    IBillOfQuantitiesRepository billsOfQuantities,
    IContractRepository contracts) : IProjectBudgetQuery
{
    public async Task<ProjectBudgetDto?> GetAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        var packages = await workPackages.ListByProjectAsync(project.Id, cancellationToken);

        var lines = new List<WorkPackageBudgetLineDto>(packages.Count);

        // Per-currency accumulators for the project-level projection.
        var committed = new Dictionary<Currency, decimal>();
        var estimatedLow = new Dictionary<Currency, decimal>();
        var estimatedHigh = new Dictionary<Currency, decimal>();
        var unpricedCount = 0;

        foreach (var package in packages.OrderBy(wp => wp.Sequence))
        {
            var line = await BuildLineAsync(package, cancellationToken);
            lines.Add(line);

            switch (line.Kind)
            {
                case BudgetLineKind.Contract:
                    Accumulate(committed, line.Committed!);
                    break;
                case BudgetLineKind.Bids:
                    foreach (var range in line.Candidates)
                    {
                        Accumulate(estimatedLow, range.Low);
                        Accumulate(estimatedHigh, range.High);
                    }

                    break;
                default:
                    // Pending (bids but no price) and None (no bids) contribute no figure.
                    unpricedCount++;
                    break;
            }
        }

        var totals = BuildTotals(committed, estimatedLow, estimatedHigh);

        return new ProjectBudgetDto(project.Id.Value, project.Name, lines, totals, unpricedCount);
    }

    private async Task<WorkPackageBudgetLineDto> BuildLineAsync(
        WorkPackage package,
        CancellationToken cancellationToken)
    {
        // 1. Awarded → the contract value wins.
        if (package.AwardedContractId is not null)
        {
            var contract = await contracts.GetByWorkPackageAsync(package.Id, cancellationToken);
            if (contract is not null)
            {
                return Line(package, BudgetLineKind.Contract, committed: ToDto(contract.Value), candidates: []);
            }
        }

        // 2. Out to bid → range of the bids' current priced BoQ totals, grouped by currency.
        var packageBids = await bids.ListByWorkPackageAsync(package.Id, cancellationToken);
        var pricedBoqs = new List<BillOfQuantities>();
        foreach (var bid in packageBids)
        {
            var current = await CurrentPricedBoqAsync(bid.Id, cancellationToken);
            if (current is not null)
            {
                pricedBoqs.Add(current);
            }
        }

        if (pricedBoqs.Count > 0)
        {
            var candidates = pricedBoqs
                .GroupBy(boq => boq.Total.Currency)
                .OrderBy(g => (int)g.Key)
                .Select(BuildRange)
                .ToList();
            return Line(package, BudgetLineKind.Bids, committed: null, candidates);
        }

        // 3. Has bids but none priced yet, or 4. no bids at all.
        var kind = packageBids.Count > 0 ? BudgetLineKind.Pending : BudgetLineKind.None;
        return Line(package, kind, committed: null, candidates: []);
    }

    /// <summary>
    /// A bid's current candidate BoQ: the latest version that has not been rejected/withdrawn and
    /// actually carries a price (positive net total). Null if the bid has no such BoQ.
    /// </summary>
    private async Task<BillOfQuantities?> CurrentPricedBoqAsync(BidId bidId, CancellationToken cancellationToken)
    {
        var versions = await billsOfQuantities.ListByBidAsync(bidId, cancellationToken);
        return versions
            .Where(boq => boq.Status is not (BoqStatus.Rejected or BoqStatus.Withdrawn))
            .Where(boq => boq.Total.Amount > 0m)
            .OrderByDescending(boq => boq.Version)
            .FirstOrDefault();
    }

    private static CandidateRangeDto BuildRange(IGrouping<Currency, BillOfQuantities> group)
    {
        var ordered = group.OrderBy(boq => boq.Total.Amount).ToList();
        var low = ordered[0];
        var high = ordered[^1];
        return new CandidateRangeDto(
            group.Key,
            ToDto(low.Total),
            ToDto(high.Total),
            ToDto(low.TotalWithVat),
            ToDto(high.TotalWithVat),
            ordered.Count);
    }

    private static IReadOnlyList<CurrencyTotalsDto> BuildTotals(
        IReadOnlyDictionary<Currency, decimal> committed,
        IReadOnlyDictionary<Currency, decimal> estimatedLow,
        IReadOnlyDictionary<Currency, decimal> estimatedHigh)
    {
        var currencies = committed.Keys
            .Concat(estimatedLow.Keys)
            .Distinct()
            .OrderBy(c => (int)c);

        return currencies
            .Select(currency =>
            {
                var c = committed.GetValueOrDefault(currency);
                var lo = estimatedLow.GetValueOrDefault(currency);
                var hi = estimatedHigh.GetValueOrDefault(currency);
                return new CurrencyTotalsDto(
                    currency,
                    new MoneyDto(c, currency),
                    new MoneyDto(lo, currency),
                    new MoneyDto(hi, currency),
                    new MoneyDto(c + lo, currency),
                    new MoneyDto(c + hi, currency));
            })
            .ToList();
    }

    private static void Accumulate(Dictionary<Currency, decimal> totals, MoneyDto money) =>
        totals[money.Currency] = totals.GetValueOrDefault(money.Currency) + money.Amount;

    private static WorkPackageBudgetLineDto Line(
        WorkPackage package,
        BudgetLineKind kind,
        MoneyDto? committed,
        IReadOnlyList<CandidateRangeDto> candidates) => new(
        package.Id.Value,
        package.Name,
        package.Status,
        package.Sequence,
        kind,
        committed,
        candidates);

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);
}
