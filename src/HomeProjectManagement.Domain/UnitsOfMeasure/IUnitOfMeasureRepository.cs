using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.UnitsOfMeasure;

/// <summary>
/// Persistence port for the <see cref="UnitOfMeasure"/> aggregate (driven port; implemented by
/// EF Core in Infrastructure). Lives beside the aggregate it serves.
/// </summary>
public interface IUnitOfMeasureRepository : IRepository<UnitOfMeasure, UnitOfMeasureId>
{
    /// <summary>
    /// The vocabulary, ordered by category then code. Inactive (retired) units are included
    /// unless <paramref name="includeInactive"/> is false.
    /// </summary>
    Task<IReadOnlyList<UnitOfMeasure>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The unit with this canonical code, or null. Used to enforce the unique-code invariant
    /// before defining a new unit.
    /// </summary>
    Task<UnitOfMeasure?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
}
