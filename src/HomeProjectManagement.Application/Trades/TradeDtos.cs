namespace HomeProjectManagement.Application.Trades;

/// <summary>
/// Read model returned to clients. <c>CreatedAt</c> comes from the aggregate's audit fields. A
/// trade is shared, project-independent reference data referenced by id from contractors and work
/// packages.
/// </summary>
public sealed record TradeDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>
/// Input for defining a canonical trade. The <c>Name</c> must be unique across the vocabulary
/// (case-insensitive); a freshly defined trade is active. <c>Code</c> is an optional short code.
/// </summary>
public sealed record DefineTradeCommand(string Name, string? Code);

/// <summary>
/// Input for editing a trade's descriptive fields. Active/inactive state is changed via the
/// dedicated activate/deactivate operations.
/// </summary>
public sealed record UpdateTradeCommand(string Name, string? Code);

/// <summary>
/// Outcome of defining a trade: either the created trade, or a conflict because a trade with the
/// same canonical name already exists (the unique-name invariant).
/// </summary>
public sealed record DefineTradeResult(TradeDto? Created, string? ConflictName)
{
    public bool IsConflict => Created is null;

    public static DefineTradeResult Success(TradeDto created) => new(created, null);

    public static DefineTradeResult Conflict(string name) => new(null, name);
}
