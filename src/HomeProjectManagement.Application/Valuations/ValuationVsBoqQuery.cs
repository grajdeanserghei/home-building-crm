using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Composes <see cref="ValuationVsBoqDto"/> from the repository ports (read-only). All figures are
/// gross (VAT-inclusive). Per active catalog item the estimate is its <c>TotalCostWithVat</c>; the actual
/// is the sum of each mapping's gross BoQ subtotal (<c>SubtotalWithVatOf</c>), scaled to the whole build via
/// <c>boq.Multiplier(apartmentUnits)</c> and converted to the catalog currency with the app-wide rate — the
/// same basis as the project budget. Note the VAT asymmetry: the catalog estimate applies one report-wide
/// VAT rate, whereas a BoQ gross subtotal sums each line's own <c>VatRate</c>, so the effective rate behind
/// estimate and actual can legitimately differ.
/// A mapping targets a section (whole), a subsection, or a single line item — its contribution is the
/// matching gross subtotal (<c>SubtotalWithVatOf</c>) or line total (<c>LineTotalWithVatOf</c>). Because a
/// whole-section subtotal already covers a section's direct lines and every subsection, a whole-section
/// mapping is full coverage; a finer (subsection or line) mapping leaves the section's uncovered remainder
/// (its direct lines and any unmapped subsections/lines) surfaced as an <c>UnattributedBoqLines</c> coverage
/// gap. Items with no mapping (the <c>%</c> catch-alls) are reported as <c>UnmappedItem</c> gaps, not as
/// −100% variance.
/// </summary>
public sealed class ValuationVsBoqQuery(
    IValuationCatalogRepository catalogs,
    IProjectRepository projects,
    IBillOfQuantitiesRepository billsOfQuantities,
    IRealBoqSelector realBoqSelector,
    IExchangeRateProvider exchangeRates,
    TimeProvider timeProvider) : IValuationVsBoqQuery
{
    public Task<ValuationVsBoqDto?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        GetAsync(projectId, new ComparisonBasis.Decided(), cancellationToken);

    public async Task<ValuationVsBoqDto?> GetAsync(Guid projectId, ComparisonBasis basis, CancellationToken cancellationToken = default)
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

        // The one active ("real") BoQ per work package under this basis. A link contributes only when its
        // BoQ is the active one for its work package — so competing BoQs of a work package collapse to a
        // single contribution and can never double-count; contributions sum only across work packages.
        var activeBoqByWorkPackage = await realBoqSelector.ResolveAsync(projectId, basis, cancellationToken);

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

        // Per (boq, section) mapped at a finer-than-whole-section granularity: the native (pricing-currency,
        // whole-build-scaled) amount covered by subsection/line mappings, so the section's uncovered
        // remainder is surfaced as a coverage gap. A whole-section mapping never appears here (granularity
        // exclusivity), so a fully-mapped section yields no gap.
        var coveredNativePerSection = new Dictionary<(Guid Boq, Guid Section), decimal>();

        foreach (var item in catalog.Items.Where(i => i.IsActive).OrderBy(i => i.Sequence))
        {
            var estimate = item.TotalCostWithVat.Amount;
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

            var actual = 0m;
            var activeContributions = 0;
            var linkDtos = new List<ValuationVsBoqLinkDto>(item.Links.Count);

            foreach (var link in item.Links)
            {
                // Only the active BoQ of each work package is "real" under this basis; a competing (or
                // non-decided) BoQ is recorded for the breakdown but contributes nothing.
                var isActive = activeBoqByWorkPackage.TryGetValue(link.WorkPackageId.Value, out var activeBoqId)
                    && activeBoqId == link.BoqId.Value;
                if (!isActive)
                {
                    linkDtos.Add(new ValuationVsBoqLinkDto(
                        link.BoqId.Value, link.SectionId.Value, link.SubsectionId?.Value, link.LineItemId?.Value,
                        BoqResolved: false, new MoneyDto(0m, currency)));
                    continue;
                }

                var boq = await LoadBoqAsync(link.BoqId);
                if (boq is null)
                {
                    linkDtos.Add(new ValuationVsBoqLinkDto(
                        link.BoqId.Value, link.SectionId.Value, link.SubsectionId?.Value, link.LineItemId?.Value,
                        BoqResolved: false, new MoneyDto(0m, currency)));
                    continue;
                }

                var native = link.LineItemId is { } lineItemId
                    ? boq.LineTotalWithVatOf(lineItemId)
                    : link.SubsectionId is { } subsectionId
                        ? boq.SubtotalWithVatOf(subsectionId)
                        : boq.SubtotalWithVatOf(link.SectionId);
                native = native.Multiply(boq.Multiplier(units));

                var contribution = ConvertTo(native, currency, asOf).Amount;
                actual += contribution;
                activeContributions++;

                // A finer-than-whole-section mapping (subsection or line) covers only part of the section;
                // accumulate its native amount so the section's uncovered remainder becomes a coverage gap.
                if (link.SubsectionId is not null || link.LineItemId is not null)
                {
                    var key = (link.BoqId.Value, link.SectionId.Value);
                    coveredNativePerSection[key] = coveredNativePerSection.GetValueOrDefault(key) + native.Amount;
                }

                linkDtos.Add(new ValuationVsBoqLinkDto(
                    link.BoqId.Value, link.SectionId.Value, link.SubsectionId?.Value, link.LineItemId?.Value,
                    BoqResolved: true, new MoneyDto(contribution, currency)));
            }

            // Mapped, but only to BoQs that are not real under this basis (all competitors lost / nothing
            // decided): report the estimate as "not realized" rather than a misleading −100% variance.
            if (activeContributions == 0)
            {
                items.Add(new ValuationVsBoqItemDto(
                    item.Id.Value, item.Sequence, item.Name, IsMapped: false,
                    new MoneyDto(estimate, currency), Actual: null, Variance: null, VariancePercentage: null,
                    linkDtos));
                gaps.Add(new ValuationCoverageGapDto(
                    "NotRealized", item.Id.Value, null, null,
                    $"\"{item.Name}\" is mapped only to BoQs that are not selected under this basis.",
                    new MoneyDto(estimate, currency)));
                continue;
            }

            mappedEstimate += estimate;
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

        // Coverage gaps: for each partially-mapped section (mapped subsection-by-subsection and/or
        // line-by-line), the section subtotal minus what those finer mappings cover is real cost no mapping
        // accounts for (its direct lines and any unmapped subsections/lines).
        var unattributed = 0m;
        foreach (var ((boqGuid, sectionGuid), coveredNative) in coveredNativePerSection)
        {
            var boq = boqCache.GetValueOrDefault(boqGuid);
            if (boq is null)
            {
                continue;
            }

            var sectionNative = boq.SubtotalWithVatOf(new SectionId(sectionGuid)).Multiply(boq.Multiplier(units)).Amount;
            var residualNative = sectionNative - coveredNative;
            if (residualNative <= 0m)
            {
                continue;
            }

            var residual = ConvertTo(new Money(residualNative, boq.PricingCurrency), currency, asOf).Amount;
            unattributed += residual;
            gaps.Add(new ValuationCoverageGapDto(
                "UnattributedBoqLines", null, boqGuid, sectionGuid,
                "BoQ cost under a partially-mapped section that no mapping covers (direct lines / unmapped subsections or lines).",
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
