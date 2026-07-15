using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.ConstructionValuations;

namespace HomeProjectManagement.Application.ConstructionValuations;

/// <summary>
/// Shared projection of a frozen <see cref="ConstructionValuation"/> snapshot to its read model. Reused by
/// the app service (detail / list / capture responses) and the progress query so the totals and EUR
/// derivation stay identical. Every value is read straight from storage — nothing is recomputed.
/// </summary>
internal static class ConstructionValuationMapper
{
    public static ConstructionValuationDto ToDto(ConstructionValuation valuation)
    {
        var ronPerEur = RonPerEur(valuation.ExchangeRate);

        return new ConstructionValuationDto(
            valuation.Id.Value,
            valuation.ValuationCatalogId.Value,
            valuation.AssessedOn,
            valuation.Appraiser,
            new ExchangeRateDto(
                valuation.ExchangeRate.BaseCurrency,
                valuation.ExchangeRate.QuoteCurrency,
                valuation.ExchangeRate.Rate,
                valuation.ExchangeRate.AsOf),
            ronPerEur,
            valuation.SourceDocument is null
                ? null
                : new SourceDocumentDto(
                    valuation.SourceDocument.FileName,
                    valuation.SourceDocument.Url,
                    valuation.SourceDocument.UploadedOn,
                    valuation.SourceDocument.UploadedBy.Value),
            valuation.SourceContentHash,
            valuation.Items.Select(ToDto).ToList(),
            BuildTotals(valuation, ronPerEur),
            valuation.CreatedOn);
    }

    /// <summary>"1 EUR = N RON" from the snapshot's pinned rate (which always relates RON↔EUR).</summary>
    public static decimal RonPerEur(ExchangeRate rate) =>
        rate.BaseCurrency == Currency.EUR ? rate.Rate : 1m / rate.Rate;

    /// <summary>The snapshot's per-currency progress totals (+ approximate EUR equivalent). Reused by the progress query.</summary>
    public static ValuationProgressTotalsDto Totals(ConstructionValuation valuation) =>
        BuildTotals(valuation, RonPerEur(valuation.ExchangeRate));

    private static ConstructionValuationItemDto ToDto(ConstructionValuationItem item) => new(
        item.Id.Value,
        item.ValuationCatalogItemId.Value,
        item.Name,
        ToDto(item.EstimatedValueWithoutVat),
        ToDto(item.EstimatedValueWithVat),
        item.CompletionPercentage,
        ToDto(item.CompletedValueWithoutVat),
        ToDto(item.CompletedValueWithVat),
        item.RemainingPercentage,
        ToDto(item.RemainingValueWithoutVat),
        ToDto(item.RemainingValueWithVat));

    // Roll the frozen items up per currency (money never sums across currencies — in practice one RON
    // entry), then add an approximate EUR equivalent of the gross totals via the pinned rate.
    private static ValuationProgressTotalsDto BuildTotals(ConstructionValuation valuation, decimal ronPerEur)
    {
        var byCurrency = valuation.Items
            .GroupBy(i => i.EstimatedValueWithoutVat.Currency)
            .OrderBy(g => (int)g.Key)
            .Select(g => new ValuationCurrencyTotalDto(
                g.Key,
                Sum(g, i => i.EstimatedValueWithoutVat, g.Key),
                Sum(g, i => i.EstimatedValueWithVat, g.Key),
                Sum(g, i => i.CompletedValueWithoutVat, g.Key),
                Sum(g, i => i.CompletedValueWithVat, g.Key),
                Sum(g, i => i.RemainingValueWithoutVat, g.Key),
                Sum(g, i => i.RemainingValueWithVat, g.Key)))
            .ToList();

        ValuationEurEquivalentDto? eur = byCurrency.Count == 0
            ? null
            : new ValuationEurEquivalentDto(
                ronPerEur,
                new MoneyDto(byCurrency.Sum(t => ToEur(t.EstimatedWithVat, ronPerEur)), Currency.EUR),
                new MoneyDto(byCurrency.Sum(t => ToEur(t.CompletedWithVat, ronPerEur)), Currency.EUR),
                new MoneyDto(byCurrency.Sum(t => ToEur(t.RemainingWithVat, ronPerEur)), Currency.EUR));

        return new ValuationProgressTotalsDto(byCurrency, eur);
    }

    private static MoneyDto Sum(
        IEnumerable<ConstructionValuationItem> items,
        Func<ConstructionValuationItem, Money> pick,
        Currency currency) =>
        new(items.Aggregate(0m, (acc, i) => acc + pick(i).Amount), currency);

    // A RON amount → EUR at "1 EUR = ronPerEur RON"; an EUR amount passes through.
    private static decimal ToEur(MoneyDto money, decimal ronPerEur) =>
        money.Currency == Currency.EUR ? money.Amount : money.Amount / ronPerEur;

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);
}
