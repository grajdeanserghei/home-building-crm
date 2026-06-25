using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.CostScenarios;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.CostScenarios;

/// <summary>
/// Composes <see cref="CostScenarioResultDto"/> (and the editor's candidate listing) from the existing
/// repository ports (read-only). For each selection the cost is the chosen bid's current priced BoQ,
/// scaled to the whole build (a per-apartment <c>deviz</c> times the project's apartment-unit count).
/// Totals are accumulated per currency because <see cref="Money"/> rejects cross-currency arithmetic,
/// and an approximate EUR-equivalent sums them via the single app-wide display rate. A chosen bid with
/// no current priced BoQ produces a line marked not-priced that contributes nothing.
/// </summary>
public sealed class CostScenarioQuery(
    ICostScenarioRepository scenarios,
    IProjectRepository projects,
    IWorkPackageRepository workPackages,
    IBidRepository bids,
    IBillOfQuantitiesRepository billsOfQuantities,
    IContractorRepository contractors,
    IExchangeRateProvider exchangeRates,
    TimeProvider timeProvider) : ICostScenarioQuery
{
    public async Task<CostScenarioResultDto?> GetAsync(Guid scenarioId, CancellationToken cancellationToken = default)
    {
        var scenario = await scenarios.GetAsync(new CostScenarioId(scenarioId), cancellationToken);
        if (scenario is null)
        {
            return null;
        }

        var project = await projects.GetAsync(scenario.ProjectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        // One conversion date for the whole rollup, so the per-line EUR column and the EUR-equivalent
        // total use the same rate.
        var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var net = new Dictionary<Currency, decimal>();
        var gross = new Dictionary<Currency, decimal>();
        var contractorNames = new Dictionary<ContractorId, string>();

        var lines = new List<ScenarioLineDto>(scenario.Selections.Count);

        foreach (var selection in scenario.Selections)
        {
            var workPackage = await workPackages.GetAsync(selection.WorkPackageId, cancellationToken);
            var bid = await bids.GetAsync(selection.BidId, cancellationToken);
            if (workPackage is null || bid is null)
            {
                // The work package or bid was deleted out from under the saved selection — skip it.
                continue;
            }

            var contractorName = await ContractorNameAsync(bid.ContractorId, contractorNames, cancellationToken);
            var boq = await CurrentPricedBoqAsync(bid.Id, cancellationToken);

            if (boq is not null)
            {
                var lineNet = boq.EffectiveTotal(project.ApartmentUnits);
                var lineGross = boq.EffectiveTotalWithVat(project.ApartmentUnits);
                Accumulate(net, lineNet);
                Accumulate(gross, lineGross);

                lines.Add(new ScenarioLineDto(
                    workPackage.Id.Value, workPackage.Name, workPackage.Sequence,
                    bid.Id.Value, bid.ContractorId.Value, contractorName, boq.Id.Value,
                    boq.Scope, boq.Multiplier(project.ApartmentUnits),
                    ToDto(lineNet), ToDto(lineGross), ToDto(ToEur(lineGross, asOf)), Priced: true));
            }
            else
            {
                lines.Add(new ScenarioLineDto(
                    workPackage.Id.Value, workPackage.Name, workPackage.Sequence,
                    bid.Id.Value, bid.ContractorId.Value, contractorName, BoqId: null,
                    BudgetScopeKind.EntireBuilding, Multiplier: 1,
                    Zero(), Zero(), new MoneyDto(0m, Currency.EUR), Priced: false));
            }
        }

        var orderedLines = lines.OrderBy(l => l.Sequence).ToList();
        var totals = BuildTotals(net, gross);
        var eurEquivalent = BuildEurEquivalent(totals, asOf);

        return new CostScenarioResultDto(
            scenario.Id.Value, scenario.ProjectId.Value, scenario.Name, scenario.Description,
            orderedLines, totals, eurEquivalent, scenario.CreatedOn);
    }

    public async Task<IReadOnlyList<ScenarioCandidateWorkPackageDto>?> GetCandidatesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        var packages = await workPackages.ListByProjectAsync(project.Id, cancellationToken);
        var contractorNames = new Dictionary<ContractorId, string>();

        var result = new List<ScenarioCandidateWorkPackageDto>(packages.Count);
        foreach (var package in packages.OrderBy(wp => wp.Sequence))
        {
            var packageBids = await bids.ListByWorkPackageAsync(package.Id, cancellationToken);

            var candidates = new List<ScenarioCandidateBidDto>();
            foreach (var bid in packageBids)
            {
                var boq = await CurrentPricedBoqAsync(bid.Id, cancellationToken);
                if (boq is null)
                {
                    continue;
                }

                var name = await ContractorNameAsync(bid.ContractorId, contractorNames, cancellationToken);
                candidates.Add(new ScenarioCandidateBidDto(
                    bid.Id.Value, bid.ContractorId.Value, name, boq.Id.Value, boq.Scope,
                    ToDto(boq.EffectiveTotal(project.ApartmentUnits)),
                    ToDto(boq.EffectiveTotalWithVat(project.ApartmentUnits))));
            }

            result.Add(new ScenarioCandidateWorkPackageDto(
                package.Id.Value, package.Name, package.Sequence, candidates));
        }

        return result;
    }

    /// <summary>
    /// A bid's current candidate BoQ: its single BoQ, provided it has not been rejected/withdrawn and
    /// actually carries a price (positive net total). Null otherwise. Mirrors the project budget rule.
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

    private async Task<string> ContractorNameAsync(
        ContractorId contractorId,
        Dictionary<ContractorId, string> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(contractorId, out var cached))
        {
            return cached;
        }

        var contractor = await contractors.GetAsync(contractorId, cancellationToken);
        var name = contractor?.Name ?? string.Empty;
        cache[contractorId] = name;
        return name;
    }

    private static IReadOnlyList<ScenarioCurrencyTotalDto> BuildTotals(
        IReadOnlyDictionary<Currency, decimal> net,
        IReadOnlyDictionary<Currency, decimal> gross)
    {
        return net.Keys
            .Concat(gross.Keys)
            .Distinct()
            .OrderBy(c => (int)c)
            .Select(currency => new ScenarioCurrencyTotalDto(
                currency,
                new MoneyDto(net.GetValueOrDefault(currency), currency),
                new MoneyDto(gross.GetValueOrDefault(currency), currency)))
            .ToList();
    }

    /// <summary>
    /// Convert the per-currency totals to EUR with the single app-wide display rate and sum them into
    /// one comparable figure. Null when there is nothing to convert. Approximate by design — the
    /// per-BoQ pinned rate remains the source of truth for a specific quote.
    /// </summary>
    private ScenarioEurEquivalentDto? BuildEurEquivalent(IReadOnlyList<ScenarioCurrencyTotalDto> totals, DateOnly asOf)
    {
        if (totals.Count == 0)
        {
            return null;
        }

        var ronPerEur = exchangeRates.GetRate(Currency.EUR, Currency.RON, asOf).Rate;

        var net = totals.Sum(t => ToEur(t.Net, asOf).Amount);
        var gross = totals.Sum(t => ToEur(t.Gross, asOf).Amount);

        return new ScenarioEurEquivalentDto(
            ronPerEur, new MoneyDto(net, Currency.EUR), new MoneyDto(gross, Currency.EUR));
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

    private Money ToEur(Money money, DateOnly asOf) =>
        money.Currency == Currency.EUR
            ? money
            : exchangeRates.GetRate(money.Currency, Currency.EUR, asOf).Convert(money);

    private static void Accumulate(Dictionary<Currency, decimal> totals, Money money) =>
        totals[money.Currency] = totals.GetValueOrDefault(money.Currency) + money.Amount;

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);

    private static MoneyDto Zero() => new(0m, Currency.RON);
}
