using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.ValuationCatalogs;

/// <summary>
/// A mapping from a <see cref="ValuationCatalogItem"/> to one section (or subsection) of a real
/// contractor <see cref="BillOfQuantities"/>, so the owners' real costs roll up per appraiser item.
/// A value object — immutable, compared by value; whole links are added and removed, never mutated.
/// </summary>
/// <remarks>
/// The three ids reference the <see cref="BillOfQuantities"/> aggregate <b>loosely by id</b> (no EF
/// navigation). Validating that they point at an existing BoQ section/subsection is the application
/// service's job; a deleted section simply drops out of the read-time rollup. A subsection link
/// <b>always carries that subsection's actual parent <see cref="SectionId"/></b> (the application service
/// populates it) — which is what lets the <see cref="ValuationCatalog"/> root enforce the
/// <b>granularity-exclusivity</b> invariant (a section is mapped either as a whole or subsection-by-
/// subsection, never both) from the link tuples alone, without consulting the BoQ. The
/// <b>no-double-count</b> invariant (each triple linked to at most one catalog item) is likewise enforced
/// by the root — the only place that sees every item's links — using this type's value equality.
/// </remarks>
public sealed class ValuationItemLink : ValueObject
{
    /// <summary>The bill of quantities the mapped section belongs to (by id).</summary>
    public BoqId BoqId { get; }

    /// <summary>
    /// The work package the mapped BoQ competes for (by id). Stamped by the application service
    /// (<c>boq → bid → workPackage</c>) so the estimate-vs-real read model can treat competing BoQs of
    /// one work package as mutually-exclusive alternatives (group by work package, pick one "real" BoQ)
    /// rather than summing them. Functionally determined by <see cref="BoqId"/>.
    /// </summary>
    public WorkPackageId WorkPackageId { get; }

    /// <summary>The mapped BoQ section (by id).</summary>
    public SectionId SectionId { get; }

    /// <summary>The mapped subsection within that section (by id), when the mapping is that fine-grained.</summary>
    public SubsectionId? SubsectionId { get; }

    public ValuationItemLink(BoqId boqId, WorkPackageId workPackageId, SectionId sectionId, SubsectionId? subsectionId = null)
    {
        BoqId = boqId;
        WorkPackageId = workPackageId;
        SectionId = sectionId;
        SubsectionId = subsectionId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        // WorkPackageId is deliberately excluded — it is derived from BoqId, not part of identity, so an
        // unlink can match on the loose triple and the no-double-count/granularity invariants are unchanged.
        yield return BoqId;
        yield return SectionId;
        yield return SubsectionId;
    }
}
