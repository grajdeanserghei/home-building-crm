using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A single priced row within a <see cref="Section"/> (optionally one level deeper, inside a
/// <see cref="Subsection"/>) — the description, quantity, unit, and unit price of one item of work
/// or material in a contractor's quote.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ
/// but is never referenced from outside it. Created, revised and re-grouped only through the
/// <see cref="BillOfQuantities"/> root, which owns every line in one flat collection. Its grouping is
/// carried by <see cref="SectionId"/> (always set) and <see cref="SubsectionId"/> (set when the line
/// sits inside a subsection, null when held directly in the section) — so moving a line between
/// containers is a plain field update on the same entity rather than a delete+insert. It references
/// its <see cref="UnitOfMeasureId"/> <b>by identity</b> (the canonical unit), never by holding the
/// unit aggregate. The <see cref="UnitPrice"/> is the <b>net (VAT-exclusive)</b> price; the
/// <see cref="VatRate"/> (21% by default) yields the gross (VAT-inclusive) figures. The
/// <see cref="LineTotal"/> and <see cref="LineTotalWithVat"/> are derived (quantity × unit price),
/// never stored as source of truth.
/// </remarks>
public sealed class LineItem : Entity<LineItemId>
{
    /// <summary>The section this line belongs to (always set), held by id.</summary>
    public SectionId SectionId { get; private set; }

    /// <summary>
    /// The subsection this line sits in, held by id; null when the line is held directly in the
    /// section rather than one level deeper inside a subsection.
    /// </summary>
    public SubsectionId? SubsectionId { get; private set; }

    /// <summary>The work or material item being priced.</summary>
    public string Description { get; private set; } = null!;

    /// <summary>How much of the item is required, in its unit of measure.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>The canonical unit this quantity is measured in (by id).</summary>
    public UnitOfMeasureId UnitOfMeasureId { get; private set; }

    /// <summary>Net price per unit (VAT-exclusive), in the BoQ's pricing currency.</summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>The VAT rate applied to this line (21% by default).</summary>
    public VatRate VatRate { get; private set; } = null!;

    /// <summary>Order within its container (the section's direct list, or its subsection).</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional remark about the line.</summary>
    public string? Notes { get; private set; }

    /// <summary>Derived gross unit price (VAT-inclusive): <c>unit price × (1 + VAT)</c>.</summary>
    public Money UnitPriceWithVat => VatRate.ApplyTo(UnitPrice);

    /// <summary>Derived net line total (VAT-exclusive): <c>quantity × unit price</c>, in the pricing currency.</summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    /// <summary>Derived gross line total (VAT-inclusive): <c>line total × (1 + VAT)</c>.</summary>
    public Money LineTotalWithVat => VatRate.ApplyTo(LineTotal);

    // EF Core materialisation constructor.
    private LineItem()
    {
    }

    // Created only by the BillOfQuantities root. SubsectionId is null for a line held directly in
    // the section, or the subsection's id for a line one level deeper.
    internal LineItem(
        LineItemId id,
        SectionId sectionId,
        SubsectionId? subsectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes) : base(id)
    {
        Id = id;
        SectionId = sectionId;
        SubsectionId = subsectionId;
        Description = NormalizeDescription(description);
        Quantity = EnsureNonNegative(quantity);
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Sequence = sequence;
        Notes = Trim(notes);
    }

    // Revised only by the BillOfQuantities root.
    internal void Revise(
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        Description = NormalizeDescription(description);
        Quantity = EnsureNonNegative(quantity);
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Sequence = sequence;
        Notes = Trim(notes);
    }

    // Re-group the line under a new container (a section's direct list when subsectionId is null,
    // otherwise the named subsection). Used by the BoQ root's move flow; the priced data is untouched
    // and the id is preserved, so persistence is a plain update.
    internal void Reassign(SectionId sectionId, SubsectionId? subsectionId)
    {
        SectionId = sectionId;
        SubsectionId = subsectionId;
    }

    // Set the ordering position only, leaving the priced data untouched. Used by the BoQ root's
    // reorder/move flow to keep a container's sequences dense (1..N) after a drag.
    internal void Resequence(int sequence) => Sequence = sequence;

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainValidationException("Line item description is required.", nameof(description));
        }

        return description.Trim();
    }

    private static decimal EnsureNonNegative(decimal quantity)
    {
        if (quantity < 0m)
        {
            throw new DomainValidationException("Line item quantity cannot be negative.", nameof(quantity));
        }

        return quantity;
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
