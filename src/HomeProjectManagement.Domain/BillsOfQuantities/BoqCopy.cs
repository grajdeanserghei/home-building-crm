namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// The result of deep-copying a <see cref="BillOfQuantities"/> via <see cref="BillOfQuantities.CopyFor"/>:
/// the new <see cref="Bill"/> plus the old→new id maps for its sections, subsections and line items. The
/// maps let a caller re-point things that referenced the source BoQ's ids (e.g. a valuation catalog's
/// links) at the freshly-minted copy — every id in the source appears exactly once as a key.
/// </summary>
public sealed record BoqCopy(
    BillOfQuantities Bill,
    IReadOnlyDictionary<SectionId, SectionId> SectionMap,
    IReadOnlyDictionary<SubsectionId, SubsectionId> SubsectionMap,
    IReadOnlyDictionary<LineItemId, LineItemId> LineItemMap);
