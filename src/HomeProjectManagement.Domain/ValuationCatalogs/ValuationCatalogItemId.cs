using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// Strongly-typed local identity for a <see cref="ValuationCatalogItem"/> within the
/// <see cref="ValuationCatalog"/> aggregate. Referenced by id from <see cref="ConstructionValuations"/>
/// snapshot items, so it survives the item's later deactivation.
/// </summary>
public readonly record struct ValuationCatalogItemId(Guid Value) : IStronglyTypedId
{
    public static ValuationCatalogItemId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
