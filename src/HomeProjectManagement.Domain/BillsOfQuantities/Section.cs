using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A grouping heading inside a single Bill of Quantities (e.g. Foundation, Structure, Roof within a
/// "La Roșu" quote) — the internal structure of one quote, distinct from a Work Package.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ but
/// is never referenced from outside it, so the BoQ root owns its lifecycle and that of its
/// <see cref="Subsection"/>s. It is a <b>heading</b>: the line items it contains are held in the
/// <see cref="BillOfQuantities"/> root's flat line collection, each tagged with this section's id
/// (and, when grouped one level deeper, a <see cref="Subsection"/> id); the section's subtotal is
/// derived by the root from those lines. Line items may sit <b>directly</b> in the section or inside a
/// <see cref="Subsection"/> (the nesting depth is fixed at one). It carries the BoQ's pricing
/// <see cref="Currency"/> so every line shares one currency.
/// </remarks>
public sealed class Section : Entity<SectionId>
{
    private readonly List<Subsection> _subsections = [];

    /// <summary>The section heading (e.g. "Foundation").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Order within the BoQ.</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional notes about the section.</summary>
    public string? Description { get; private set; }

    /// <summary>The BoQ's pricing currency; every line item in the section shares it.</summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// The subsections of this section (internal entities), an optional second level of grouping.
    /// Mutated only through the <see cref="BillOfQuantities"/> root; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<Subsection> Subsections => _subsections.AsReadOnly();

    // EF Core materialisation constructor.
    private Section()
    {
    }

    // Created only by the BillOfQuantities root.
    internal Section(SectionId id, string name, int sequence, string? description, Currency currency) : base(id)
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

    internal Subsection AddSubsection(string name, int sequence, string? description)
    {
        var subsection = new Subsection(SubsectionId.New(), name, sequence, description, Currency);
        _subsections.Add(subsection);
        return subsection;
    }

    internal bool UpdateSubsection(SubsectionId subsectionId, string name, int sequence, string? description)
    {
        var subsection = FindSubsection(subsectionId);
        if (subsection is null)
        {
            return false;
        }

        subsection.Update(name, sequence, description);
        return true;
    }

    internal bool RemoveSubsection(SubsectionId subsectionId)
    {
        var subsection = FindSubsection(subsectionId);
        if (subsection is null)
        {
            return false;
        }

        _subsections.Remove(subsection);
        return true;
    }

    internal Subsection? FindSubsection(SubsectionId subsectionId) =>
        _subsections.FirstOrDefault(s => s.Id == subsectionId);

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Section name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
