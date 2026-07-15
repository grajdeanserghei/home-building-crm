namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>Lifecycle of a <see cref="ValuationCatalog"/>.</summary>
public enum ValuationCatalogStatus
{
    /// <summary>Being assembled; not yet the project's baseline.</summary>
    Draft,

    /// <summary>The project's active valuation baseline that snapshots are captured against.</summary>
    Active
}
