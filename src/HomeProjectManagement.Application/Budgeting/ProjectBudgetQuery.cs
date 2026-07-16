using HomeProjectManagement.Application.Abstractions;
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
/// All figures are VAT-inclusive (gross): bid ranges use the BoQ's <c>TotalWithVat</c>, and the
/// contract's net agreed value is grossed up by the accepted BoQ's effective VAT ratio. Figures are
/// also scaled to the whole build: a BoQ a supplier priced <i>per apartment</i> is multiplied by the
/// project's apartment-unit count, so one quote covers every (identical) apartment without duplication.
/// </summary>
public sealed class ProjectBudgetQuery(
    IProjectRepository projects,
    IWorkPackageRepository workPackages,
    IBidRepository bids,
    IBillOfQuantitiesRepository billsOfQuantities,
    IContractRepository contracts,
    IExchangeRateProvider exchangeRates,
    TimeProvider timeProvider) : IProjectBudgetQuery
{
    public async Task<ProjectBudgetDto?> GetAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        var packages = await workPackages.ListByProjectAsync(project.Id, cancellationToken);

        // One conversion date for the whole rollup, so the per-line EUR column and the EUR-equivalent
        // total use the same rate.
        var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var lines = new List<WorkPackageBudgetLineDto>(packages.Count);

        // Per-currency accumulators for the project-level projection.
        var committed = new Dictionary<Currency, decimal>();
        var estimatedLow = new Dictionary<Currency, decimal>();
        var estimatedHigh = new Dictionary<Currency, decimal>();
        var unpricedCount = 0;

        foreach (var package in packages.OrderBy(wp => wp.Sequence))
        {
            var line = await BuildLineAsync(package, project.ApartmentUnits, cancellationToken);
            line = line with { EurEquivalent = LineEur(line, asOf) };
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
        var eurEquivalent = BuildEurEquivalent(totals, asOf);

        return new ProjectBudgetDto(
            project.Id.Value, project.Name, project.ApartmentUnits, lines, totals, unpricedCount, eurEquivalent);
    }

    /// <summary>
    /// One work-package line's figure as an approximate EUR (gross) band: an awarded line's single
    /// value (low == high), a bid line's candidate range converted and summed across currencies, or
    /// null when the line has no figure.
    /// </summary>
    private EurBandDto? LineEur(WorkPackageBudgetLineDto line, DateOnly asOf)
    {
        if (line.Kind == BudgetLineKind.Contract && line.Committed is not null)
        {
            var eur = ToEur(line.Committed, asOf);
            return new EurBandDto(eur, eur);
        }

        if (line.Kind == BudgetLineKind.Bids && line.Candidates.Count > 0)
        {
            var low = line.Candidates.Sum(c => ToEur(c.Low, asOf).Amount);
            var high = line.Candidates.Sum(c => ToEur(c.High, asOf).Amount);
            return new EurBandDto(new MoneyDto(low, Currency.EUR), new MoneyDto(high, Currency.EUR));
        }

        return null;
    }

    /// <summary>
    /// Convert the per-currency totals to EUR with the single app-wide display rate and sum them into
    /// one comparable figure. Null when there is nothing to convert. Approximate by design — the
    /// per-BoQ pinned rate remains the source of truth for a specific quote.
    /// </summary>
    private EurEquivalentDto? BuildEurEquivalent(IReadOnlyList<CurrencyTotalsDto> totals, DateOnly asOf)
    {
        if (totals.Count == 0)
        {
            return null;
        }

        var ronPerEur = exchangeRates.GetRate(Currency.EUR, Currency.RON, asOf).Rate;

        decimal SumEur(Func<CurrencyTotalsDto, MoneyDto> pick) =>
            totals.Sum(t => ToEur(pick(t), asOf).Amount);

        var eurTotals = new CurrencyTotalsDto(
            Currency.EUR,
            new MoneyDto(SumEur(t => t.Committed), Currency.EUR),
            new MoneyDto(SumEur(t => t.EstimatedLow), Currency.EUR),
            new MoneyDto(SumEur(t => t.EstimatedHigh), Currency.EUR),
            new MoneyDto(SumEur(t => t.ProjectedLow), Currency.EUR),
            new MoneyDto(SumEur(t => t.ProjectedHigh), Currency.EUR));

        return new EurEquivalentDto(ronPerEur, eurTotals);
    }

    private MoneyDto ToEur(MoneyDto money, DateOnly asOf)
    {
        if (money.Currency == Currency.EUR)
        {
            return money;
        }

        var converted = exchangeRates
            .GetRate(money.Currency, Currency.EUR, asOf)
            .Convert(new Money(money.Amount, money.Currency));
        return new MoneyDto(converted.Amount, converted.Currency);
    }

    private async Task<WorkPackageBudgetLineDto> BuildLineAsync(
        WorkPackage package,
        int apartmentUnits,
        CancellationToken cancellationToken)
    {
        // 1. Awarded → the contract value (grossed up to VAT-inclusive) wins.
        if (package.AwardedContractId is not null)
        {
            var contract = await contracts.GetByWorkPackageAsync(package.Id, cancellationToken);
            if (contract is not null)
            {
                var committed = await ContractGrossAsync(contract, apartmentUnits, cancellationToken);
                return Line(package, BudgetLineKind.Contract, committed: ToDto(committed), candidates: []);
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
                .Select(g => BuildRange(g, apartmentUnits))
                .ToList();
            return Line(package, BudgetLineKind.Bids, committed: null, candidates);
        }

        // 3. Has bids but none priced yet, or 4. no bids at all.
        var kind = packageBids.Count > 0 ? BudgetLineKind.Pending : BudgetLineKind.None;
        return Line(package, kind, committed: null, candidates: []);
    }

    /// <summary>
    /// A bid's current candidate BoQ: its single BoQ, provided it has not been rejected/withdrawn and
    /// actually carries a price (positive net total). Null if the bid has no such BoQ.
    /// </summary>
    private async Task<BillOfQuantities?> CurrentPricedBoqAsync(BidId bidId, CancellationToken cancellationToken)
    {
        var boq = await billsOfQuantities.GetByBidAsync(bidId, cancellationToken);
        if (boq is null
            || boq.Status is BoqStatus.Rejected or BoqStatus.Withdrawn
            || boq.Total.Amount <= 0m)
        {
            return null;
        }

        return boq;
    }

    private static CandidateRangeDto BuildRange(IGrouping<Currency, BillOfQuantities> group, int apartmentUnits)
    {
        // Ranked by the effective VAT-inclusive total — each BoQ scaled to the whole build, so a
        // per-apartment quote is compared against an entire-building one on equal terms.
        var ordered = group.OrderBy(boq => boq.EffectiveTotalWithVat(apartmentUnits).Amount).ToList();
        return new CandidateRangeDto(
            group.Key,
            ToDto(ordered[0].EffectiveTotalWithVat(apartmentUnits)),
            ToDto(ordered[^1].EffectiveTotalWithVat(apartmentUnits)),
            ordered.Count);
    }

    /// <summary>
    /// The contract's agreed value made VAT-inclusive. <c>Contract.Value</c> is net and carries no VAT
    /// rate, so the gross is derived by applying the accepted BoQ's <i>effective</i> VAT ratio
    /// (<c>TotalWithVat / Total</c>) — which honours the BoQ's actual per-line VAT mix and any
    /// negotiated value. Falls back to the agreed value when the BoQ is missing or has a zero total.
    /// </summary>
    private async Task<Money> ContractGrossAsync(Contract contract, int apartmentUnits, CancellationToken cancellationToken)
    {
        var boq = await billsOfQuantities.GetAsync(contract.AcceptedBoqId, cancellationToken);
        if (boq is not null && boq.Total.Amount != 0m)
        {
            var vatRatio = boq.TotalWithVat.Amount / boq.Total.Amount;
            // Scale to the whole build the same way the accepted BoQ is: a per-apartment deviz counts
            // once per apartment unit (the contract value defaults to that per-apartment total).
            return contract.Value.Multiply(vatRatio).Multiply(boq.Multiplier(apartmentUnits));
        }

        return contract.Value;
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

    // The EUR band is filled in by the caller (LineEur) once the native figure is known.
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
        candidates,
        EurEquivalent: null);

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);
}
