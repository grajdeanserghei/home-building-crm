using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.UnitsOfMeasure.Events;

namespace HomeProjectManagement.Domain.UnitsOfMeasure;

/// <summary>
/// A canonical unit used by Bill-of-Quantities line items (m³, m², m, pcs, kg, hrs). A
/// <b>controlled reference vocabulary</b> — not free text — so the same unit never appears as
/// m³/mc/m3/"cubic meter" across contractors' quotes, which would break side-by-side
/// comparison. Source <c>deviz</c> abbreviations (Romanian: mc, mp, ml, buc, to) are held as
/// <see cref="Aliases"/> that normalize onto this one canonical unit.
/// </summary>
/// <remarks>
/// Aggregate root referenced <b>by id</b> from line items (inside the Bill of Quantities
/// aggregate), never held directly. Seeded with the standard construction units and only
/// extended by an admin. A unit is <b>retired via <see cref="Deactivate"/></b> rather than
/// deleted, since historical line items may still reference it. State changes go through
/// intention-revealing methods; construction goes through the <see cref="Define"/> factory.
/// </remarks>
public sealed class UnitOfMeasure : AggregateRoot<UnitOfMeasureId>
{
    private readonly List<string> _aliases = [];

    /// <summary>Canonical symbol, unique across all units (e.g. <c>m³</c>, <c>pcs</c>).</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Human-readable name (e.g. "Cubic metre").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>The kind of quantity this unit measures.</summary>
    public UnitCategory Category { get; private set; }

    /// <summary>
    /// Source/abbreviation forms that normalize onto this unit (Romanian deviz: mc, mp, ml …),
    /// stored lower-cased for case-insensitive matching. Never contains the canonical code.
    /// Mutated only through <see cref="AddAlias"/>/<see cref="RemoveAlias"/>/<see cref="SetAliases"/>.
    /// </summary>
    public IReadOnlyCollection<string> Aliases => _aliases.AsReadOnly();

    /// <summary>Whether the unit may be assigned to new line items. Retire via <see cref="Deactivate"/>.</summary>
    public bool IsActive { get; private set; }

    // EF Core materialisation constructor.
    private UnitOfMeasure()
    {
    }

    private UnitOfMeasure(UnitOfMeasureId id, string code, string name, UnitCategory category) : base(id)
    {
        Id = id;
        Code = code;
        Name = name;
        Category = category;
    }

    /// <summary>
    /// Factory: define a new canonical unit, validating its invariants. <paramref name="now"/>
    /// is supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain.
    /// A freshly defined unit is active. Aliases are normalized (trimmed, lower-cased,
    /// de-duplicated) and may not duplicate the canonical code. Global code uniqueness is the
    /// caller's responsibility (enforced by the application service + a unique DB index).
    /// </summary>
    public static UnitOfMeasure Define(
        string code,
        string name,
        UnitCategory category,
        DateTimeOffset now,
        IEnumerable<string>? aliases = null)
    {
        var unit = new UnitOfMeasure(UnitOfMeasureId.New(), NormalizeCode(code), NormalizeName(name), category)
        {
            IsActive = true
        };

        if (aliases is not null)
        {
            foreach (var alias in aliases)
            {
                unit.AddAlias(alias);
            }
        }

        unit.Raise(new UnitOfMeasureDefined(unit.Id, unit.Code, unit.Name, category, now));
        return unit;
    }

    /// <summary>Rename the unit (its descriptive name, not the canonical code).</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Reclassify the unit's measurement category.</summary>
    public void Recategorize(UnitCategory category) => Category = category;

    /// <summary>
    /// Register a source abbreviation that should normalize onto this unit. No-op if it is
    /// already present (case-insensitively) or equal to the canonical code.
    /// </summary>
    public void AddAlias(string alias)
    {
        var normalized = NormalizeAlias(alias);
        if (string.Equals(normalized, Code, StringComparison.OrdinalIgnoreCase) || _aliases.Contains(normalized))
        {
            return;
        }

        _aliases.Add(normalized);
    }

    /// <summary>Remove a previously registered alias. No-op if absent.</summary>
    public void RemoveAlias(string alias) => _aliases.Remove(NormalizeAlias(alias));

    /// <summary>Replace the whole alias set in one operation, applying the same normalization rules.</summary>
    public void SetAliases(IEnumerable<string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);

        _aliases.Clear();
        foreach (var alias in aliases)
        {
            AddAlias(alias);
        }
    }

    /// <summary>
    /// True if <paramref name="token"/> is this unit's canonical code or one of its aliases
    /// (case-insensitive). Used to normalize an incoming deviz unit onto the canonical vocabulary.
    /// </summary>
    public bool Recognizes(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var candidate = token.Trim();
        return string.Equals(candidate, Code, StringComparison.OrdinalIgnoreCase)
               || _aliases.Contains(candidate.ToLowerInvariant());
    }

    /// <summary>Retire the unit so it is no longer offered for new line items, without deleting it.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Raise(new UnitOfMeasureActivationChanged(Id, false, now));
    }

    /// <summary>Bring a retired unit back into use.</summary>
    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Raise(new UnitOfMeasureActivationChanged(Id, true, now));
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Unit of measure code is required.", nameof(code));
        }

        return code.Trim();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit of measure name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string NormalizeAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("Alias must not be blank.", nameof(alias));
        }

        return alias.Trim().ToLowerInvariant();
    }
}
