using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.ConstructionValuations;
using HomeProjectManagement.Domain.ConstructionValuations;
using HomeProjectManagement.Domain.ValuationCatalogs;

namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Composes <see cref="ValuationProgressSeriesDto"/> from the repository ports (read-only). Every value is
/// read straight from the frozen snapshots — nothing is recomputed. Produces both the per-snapshot totals
/// over time and each catalog item's completion series across the snapshots that assessed it.
/// </summary>
public sealed class ValuationProgressQuery(
    IValuationCatalogRepository catalogs,
    IConstructionValuationRepository valuations) : IValuationProgressQuery
{
    public async Task<ValuationProgressSeriesDto?> GetSeriesAsync(Guid catalogId, CancellationToken cancellationToken = default)
    {
        var catalog = await catalogs.GetAsync(new ValuationCatalogId(catalogId), cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        // Oldest first (the repository already orders by AssessedOn).
        var snapshots = await valuations.ListByCatalogAsync(catalog.Id, cancellationToken);

        var snapshotDtos = snapshots
            .Select(v => new ValuationProgressSnapshotDto(
                v.Id.Value,
                v.AssessedOn,
                v.Appraiser,
                ConstructionValuationMapper.RonPerEur(v.ExchangeRate),
                ConstructionValuationMapper.Totals(v)))
            .ToList();

        // Regroup the frozen rows by catalog item so each item's completion can be charted over time.
        var itemSeries = snapshots
            .SelectMany(v => v.Items.Select(i => (Snapshot: v, Item: i)))
            .GroupBy(x => x.Item.ValuationCatalogItemId.Value)
            .Select(g =>
            {
                var points = g
                    .OrderBy(x => x.Snapshot.AssessedOn)
                    .Select(x => new ValuationItemProgressPointDto(
                        x.Snapshot.Id.Value,
                        x.Snapshot.AssessedOn,
                        x.Item.CompletionPercentage,
                        ToDto(x.Item.EstimatedValueWithVat),
                        ToDto(x.Item.CompletedValueWithVat),
                        ToDto(x.Item.RemainingValueWithVat)))
                    .ToList();

                // Names are denormalized per snapshot and can drift — show the most recent one.
                var latestName = g.OrderByDescending(x => x.Snapshot.AssessedOn).First().Item.Name;
                return new ValuationItemProgressDto(g.Key, latestName, points);
            })
            .ToList();

        return new ValuationProgressSeriesDto(catalog.Id.Value, snapshotDtos, itemSeries);
    }

    private static MoneyDto ToDto(Domain.Common.ValueObjects.Money money) => new(money.Amount, money.Currency);
}
