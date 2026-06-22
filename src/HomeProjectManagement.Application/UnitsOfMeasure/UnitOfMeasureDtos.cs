using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Application.UnitsOfMeasure;

/// <summary>
/// Read model returned to clients. <c>CreatedAt</c> comes from the aggregate's audit fields;
/// <c>Aliases</c> are the normalized source abbreviations that map onto this canonical unit.
/// </summary>
public sealed record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Name,
    UnitCategory Category,
    IReadOnlyCollection<string> Aliases,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>
/// Input for defining a canonical unit. The <c>Code</c> must be unique across the vocabulary;
/// a freshly defined unit is active. <c>Aliases</c> are optional source abbreviations.
/// </summary>
public sealed record DefineUnitOfMeasureCommand(
    string Code,
    string Name,
    UnitCategory Category,
    IReadOnlyCollection<string>? Aliases);

/// <summary>
/// Input for editing a unit's descriptive fields. The canonical <c>Code</c> is intentionally
/// immutable — it is the stable identity of the vocabulary entry that line items normalize onto;
/// active/inactive state is changed via the dedicated activate/deactivate operations.
/// </summary>
public sealed record UpdateUnitOfMeasureCommand(
    string Name,
    UnitCategory Category,
    IReadOnlyCollection<string>? Aliases);

/// <summary>
/// Outcome of defining a unit: either the created unit, or a conflict because a unit with the
/// same canonical code already exists (the unique-code invariant).
/// </summary>
public sealed record DefineUnitOfMeasureResult(UnitOfMeasureDto? Created, string? ConflictCode)
{
    public bool IsConflict => Created is null;

    public static DefineUnitOfMeasureResult Success(UnitOfMeasureDto created) => new(created, null);

    public static DefineUnitOfMeasureResult Conflict(string code) => new(null, code);
}
