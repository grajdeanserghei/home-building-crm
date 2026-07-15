using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// A mapping from a <see cref="ValuationCatalogItem"/> to one section (or subsection) of a real
/// contractor <see cref="BillOfQuantities"/>, so the owners' real costs roll up per appraiser item.
/// A value object — immutable, compared by value; whole links are added and removed, never mutated.
/// </summary>
/// <remarks>
/// The three ids reference the <see cref="BillOfQuantities"/> aggregate <b>loosely by id</b> (no EF
/// navigation). Validating that they point at an existing BoQ section/subsection is the application
/// service's job; a deleted section simply drops out of the read-time rollup. The <b>no-double-count</b>
/// invariant (each triple linked to at most one catalog item) is enforced by the <see cref="ValuationCatalog"/>
/// root — the only place that sees every item's links — using this type's value equality.
/// </remarks>
public sealed class ValuationItemLink : ValueObject
{
    /// <summary>The bill of quantities the mapped section belongs to (by id).</summary>
    public BoqId BoqId { get; }

    /// <summary>The mapped BoQ section (by id).</summary>
    public SectionId SectionId { get; }

    /// <summary>The mapped subsection within that section (by id), when the mapping is that fine-grained.</summary>
    public SubsectionId? SubsectionId { get; }

    public ValuationItemLink(BoqId boqId, SectionId sectionId, SubsectionId? subsectionId = null)
    {
        BoqId = boqId;
        SectionId = sectionId;
        SubsectionId = subsectionId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BoqId;
        yield return SectionId;
        yield return SubsectionId;
    }
}
