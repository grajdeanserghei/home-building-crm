using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>Strongly-typed identity for the <see cref="ValuationCatalog"/> aggregate root.</summary>
public readonly record struct ValuationCatalogId(Guid Value) : IStronglyTypedId
{
    public static ValuationCatalogId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
