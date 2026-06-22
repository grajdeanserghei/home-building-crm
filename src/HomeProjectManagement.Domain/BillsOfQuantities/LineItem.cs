using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A single priced row within a <see cref="Section"/> — the description, quantity, unit, and unit
/// price of one item of work or material in a contractor's quote.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ
/// but is never referenced from outside it. Created and revised only through the
/// <see cref="BillOfQuantities"/> root. It references its <see cref="UnitOfMeasureId"/>
/// <b>by identity</b> (the canonical unit), never by holding the unit aggregate. The
/// <see cref="LineTotal"/> is derived (quantity × unit price), never stored as source of truth.
/// </remarks>
public sealed class LineItem : Entity<LineItemId>
{
    /// <summary>The work or material item being priced.</summary>
    public string Description { get; private set; } = null!;

    /// <summary>How much of the item is required, in its unit of measure.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>The canonical unit this quantity is measured in (by id).</summary>
    public UnitOfMeasureId UnitOfMeasureId { get; private set; }

    /// <summary>Price per unit, in the BoQ's pricing currency.</summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>Order within the section.</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional remark about the line.</summary>
    public string? Notes { get; private set; }

    /// <summary>Derived line total: <c>quantity × unit price</c>, in the pricing currency.</summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    // EF Core materialisation constructor.
    private LineItem()
    {
    }

    // Created only by the Section (which is itself driven by the BillOfQuantities root).
    internal LineItem(
        LineItemId id,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        int sequence,
        string? notes) : base(id)
    {
        Id = id;
        Description = NormalizeDescription(description);
        Quantity = EnsureNonNegative(quantity);
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        Sequence = sequence;
        Notes = Trim(notes);
    }

    // Revised only by the Section.
    internal void Revise(
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        int sequence,
        string? notes)
    {
        Description = NormalizeDescription(description);
        Quantity = EnsureNonNegative(quantity);
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        Sequence = sequence;
        Notes = Trim(notes);
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Line item description is required.", nameof(description));
        }

        return description.Trim();
    }

    private static decimal EnsureNonNegative(decimal quantity)
    {
        if (quantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Line item quantity cannot be negative.");
        }

        return quantity;
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
