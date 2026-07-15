using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Composes <see cref="ValuationVsBoqDto"/> from the repository ports (read-only). Per active catalog item
/// the estimate is its net <c>TotalCostWithoutVat</c>; the actual is the sum of each mapping's BoQ subtotal
/// (<c>SubtotalOf</c> — net, VAT-exclusive), scaled to the whole build via <c>boq.Multiplier(apartmentUnits)</c>
/// and converted to the catalog currency with the app-wide rate — the same basis as the project budget.
/// Because a whole-section subtotal already covers a section's direct lines and every subsection, a
/// whole-section mapping is full coverage; a subsection mapping leaves the section's direct lines (and any
/// unmapped subsections) uncovered, which is surfaced as an <c>UnattributedBoqLines</c> coverage gap. Items
/// with no mapping (the <c>%</c> catch-alls) are reported as <c>UnmappedItem</c> gaps, not as −100% variance.
/// </summary>
public sealed class ValuationVsBoqQuery(
    IValuationCatalogRepository catalogs,
    IProjectRepository projects,
    IBillOfQuantitiesRepository billsOfQuantities,
    IExchangeRateProvider exchangeRates,
    TimeProvider timeProvider) : IValuationVsBoqQuery
{
    public async Task<ValuationVsBoqDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var catalog = await catalogs.GetByProjectAsync(new ProjectId(projectId), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        var project = await projects.GetAsync(catalog.ProjectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var currency = catalog.Currency;
        var units = project.ApartmentUnits;
        // One conversion date for the whole rollup.
        var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var ronPerEur = exchangeRates.GetRate(Currency.EUR, Currency.RON, asOf).Rate;

        // Load each referenced BoQ at most once; a missing (deleted) BoQ resolves to null and drops out.
        var boqCache = new Dictionary<Guid, BillOfQuantities?>();
        async Task<BillOfQuantities?> LoadBoqAsync(BoqId id)
        {
            if (!boqCache.TryGetValue(id.Value, out var cached))
            {
                cached = await billsOfQuantities.GetAsync(id, cancellationToken);
                boqCache[id.Value] = cached;
            }

            return cached;
        }

        var items = new List<ValuationVsBoqItemDto>();
        var gaps = new List<ValuationCoverageGapDto>();

        var totalEstimate = 0m;
        var mappedEstimate = 0m;
        var totalActual = 0m;

        // Per (boq, section) mapped subsection-by-subsection: which subsections are covered — for the
        // direct-line coverage gap. A whole-section mapping never appears here (granularity exclusivity).
        var subsectionCoverage = new Dictionary<(Guid Boq, Guid Section), List<Guid>>();

        foreach (var item in catalog.Items.Where(i => i.IsActive).OrderBy(i => i.Sequence))
        {
            var estimate = item.TotalCostWithoutVat.Amount;
            totalEstimate += estimate;

            if (item.Links.Count == 0)
            {
                // A catch-all with no mapping: report the whole estimate as unmapped, not as −100%.
                items.Add(new ValuationVsBoqItemDto(
                    item.Id.Value, item.Sequence, item.Name, IsMapped: false,
                    new MoneyDto(estimate, currency), Actual: null, Variance: null, VariancePercentage: null,
                    Links: []));
                gaps.Add(new ValuationCoverageGapDto(
                    "UnmappedItem", item.Id.Value, null, null,
                    $"\"{item.Name}\" is not mapped to any BoQ section.",
                    new MoneyDto(estimate, currency)));
                continue;
            }

            mappedEstimate += estimate;

            var actual = 0m;
            var linkDtos = new List<ValuationVsBoqLinkDto>(item.Links.Count);

            foreach (var link in item.Links)
            {
                var boq = await LoadBoqAsync(link.BoqId);
                if (boq is null)
                {
                    linkDtos.Add(new ValuationVsBoqLinkDto(
                        link.BoqId.Value, link.SectionId.Value, link.SubsectionId?.Value,
                        BoqResolved: false, new MoneyDto(0m, currency)));
                    continue;
                }

                var native = link.SubsectionId is { } subsectionId
                    ? boq.SubtotalOf(subsectionId)
                    : boq.SubtotalOf(link.SectionId);
                native = native.Multiply(boq.Multiplier(units));

                var contribution = ConvertTo(native, currency, asOf).Amount;
                actual += contribution;

                if (link.SubsectionId is { } sub)
                {
                    var key = (link.BoqId.Value, link.SectionId.Value);
                    if (!subsectionCoverage.TryGetValue(key, out var covered))
                    {
                        subsectionCoverage[key] = covered = [];
                    }

                    covered.Add(sub.Value);
                }

                linkDtos.Add(new ValuationVsBoqLinkDto(
                    link.BoqId.Value, link.SectionId.Value, link.SubsectionId?.Value,
                    BoqResolved: true, new MoneyDto(contribution, currency)));
            }

            totalActual += actual;
            var variance = actual - estimate;
            decimal? variancePct = estimate != 0m ? variance / estimate * 100m : null;

            items.Add(new ValuationVsBoqItemDto(
                item.Id.Value, item.Sequence, item.Name, IsMapped: true,
                new MoneyDto(estimate, currency),
                new MoneyDto(actual, currency),
                new MoneyDto(variance, currency),
                variancePct,
                linkDtos));
        }

        // Direct-line coverage gaps: for each subsection-mapped section, the section subtotal minus the
        // covered subsections is real cost no mapping accounts for (its direct lines + unmapped subsections).
        var unattributed = 0m;
        foreach (var ((boqGuid, sectionGuid), coveredSubsections) in subsectionCoverage)
        {
            var boq = boqCache.GetValueOrDefault(boqGuid);
            if (boq is null)
            {
                continue;
            }

            var sectionId = new SectionId(sectionGuid);
            var sectionNative = boq.SubtotalOf(sectionId).Multiply(boq.Multiplier(units)).Amount;
            var coveredNative = coveredSubsections
                .Sum(s => boq.SubtotalOf(new SubsectionId(s)).Multiply(boq.Multiplier(units)).Amount);

            var residualNative = sectionNative - coveredNative;
            if (residualNative <= 0m)
            {
                continue;
            }

            var residual = ConvertTo(new Money(residualNative, boq.PricingCurrency), currency, asOf).Amount;
            unattributed += residual;
            gaps.Add(new ValuationCoverageGapDto(
                "UnattributedBoqLines", null, boqGuid, sectionGuid,
                "BoQ cost under a subsection-mapped section that no mapping covers (direct lines / unmapped subsections).",
                new MoneyDto(residual, currency)));
        }

        var totalVariance = totalActual - mappedEstimate;
        decimal? totalVariancePct = mappedEstimate != 0m ? totalVariance / mappedEstimate * 100m : null;
        var coveragePct = totalEstimate != 0m ? mappedEstimate / totalEstimate * 100m : 100m;

        var totals = new ValuationVsBoqTotalsDto(
            new MoneyDto(totalEstimate, currency),
            new MoneyDto(mappedEstimate, currency),
            new MoneyDto(totalActual, currency),
            new MoneyDto(totalVariance, currency),
            totalVariancePct,
            coveragePct,
            new MoneyDto(unattributed, currency));

        return new ValuationVsBoqDto(
            project.Id.Value, catalog.Id.Value, currency, ronPerEur, items, gaps, totals);
    }

    // Convert a BoQ subtotal into the catalog currency with the app-wide rate; same currency passes through.
    private Money ConvertTo(Money money, Currency target, DateOnly asOf) =>
        money.Currency == target
            ? money
            : exchangeRates.GetRate(money.Currency, target, asOf).Convert(money);
}
