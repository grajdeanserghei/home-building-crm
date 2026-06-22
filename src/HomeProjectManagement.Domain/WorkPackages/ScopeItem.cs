using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// An owner-defined sub-scope of a single <see cref="WorkPackage"/> (e.g. within "Instalații
/// termice": Încălzire pardoseală, Cameră tehnică gaz, Ventilare cu recuperare). Defined up front
/// as part of your own scoping — distinct from a BoQ <c>Section</c>, which is one contractor's
/// internal breakdown of their quote.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Work Package aggregate</b>: it has identity within the work package
/// but no lifecycle of its own, so the work package root owns it entirely and is the only thing that
/// creates, updates, or removes one. Its <see cref="Requirement"/> (Mandatory/Optional) drives
/// "what can we drop if money is tight?". A BoQ section may reference it by id for per-scope-item
/// rollup — a loose reference validated in the application service, not an EF navigation.
/// </remarks>
public sealed class ScopeItem : Entity<ScopeItemId>
{
    /// <summary>The sub-scope name (e.g. "Ventilare cu recuperare"). Unique within the work package.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional scope notes.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether this sub-scope is Mandatory or Optional.</summary>
    public ScopeItemRequirement Requirement { get; private set; }

    /// <summary>Display order within the work package.</summary>
    public int Sequence { get; private set; }

    // EF Core materialisation constructor.
    private ScopeItem()
    {
    }

    // Created only by the WorkPackage root (see WorkPackage.AddScopeItem).
    internal ScopeItem(
        ScopeItemId id,
        string name,
        ScopeItemRequirement requirement,
        int sequence,
        string? description) : base(id)
    {
        Id = id;
        Name = NormalizeName(name);
        Requirement = requirement;
        Sequence = sequence;
        Description = Trim(description);
    }

    // Updated only by the WorkPackage root (see WorkPackage.UpdateScopeItem).
    internal void Update(string name, ScopeItemRequirement requirement, int sequence, string? description)
    {
        Name = NormalizeName(name);
        Requirement = requirement;
        Sequence = sequence;
        Description = Trim(description);
    }

    internal static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Scope item name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
