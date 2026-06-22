namespace HomeProjectManagement.Application.UnitsOfMeasure;

/// <summary>
/// Driving (primary) port for unit-of-measure use cases. The minimal-API endpoints in ApiService
/// are the adapter that calls this; the host never touches EF Core or the domain directly.
/// </summary>
public interface IUnitOfMeasureAppService
{
    /// <summary>The vocabulary, ordered by category then code; retired units included by default.</summary>
    Task<IReadOnlyList<UnitOfMeasureDto>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    Task<UnitOfMeasureDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Define a unit. Reports a conflict if the canonical code is already taken.</summary>
    Task<DefineUnitOfMeasureResult> DefineAsync(
        DefineUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Edit a unit's name, category, and aliases. Returns null if it does not exist.</summary>
    Task<UnitOfMeasureDto?> UpdateAsync(
        Guid id,
        UpdateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Retire or reinstate a unit. Returns false if it does not exist.</summary>
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
