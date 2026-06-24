using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A fixed second-level grouping heading inside a <see cref="Section"/> (e.g. "Excavation" and
/// "Reinforcement" within a Foundation section). The nesting depth is fixed: a subsection groups line
/// items but never holds further subsections.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ but
/// is never referenced from outside it, so the BoQ root (via its owning <see cref="Section"/>) owns
/// its lifecycle. It is a <b>heading only</b> — the line items it groups are held in the
/// <see cref="BillOfQuantities"/> root's flat line collection, each tagged with this subsection's id
/// (see <see cref="LineItem.SubsectionId"/>); the subsection's subtotal is derived by the root from
/// those lines. It carries the BoQ's pricing <see cref="Currency"/> for consistency with its parent
/// section.
/// </remarks>
public sealed class Subsection : Entity<SubsectionId>
{
    /// <summary>The subsection heading (e.g. "Excavation").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Order within the parent section.</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional notes about the subsection.</summary>
    public string? Description { get; private set; }

    /// <summary>The BoQ's pricing currency; every line item grouped under it shares it.</summary>
    public Currency Currency { get; private set; }

    // EF Core materialisation constructor.
    private Subsection()
    {
    }

    // Created only by the owning Section (itself driven by the BillOfQuantities root).
    internal Subsection(SubsectionId id, string name, int sequence, string? description, Currency currency) : base(id)
    {
        Id = id;
        Name = NormalizeName(name);
        Sequence = sequence;
        Description = Trim(description);
        Currency = currency;
    }

    internal void Update(string name, int sequence, string? description)
    {
        Name = NormalizeName(name);
        Sequence = sequence;
        Description = Trim(description);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Subsection name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
