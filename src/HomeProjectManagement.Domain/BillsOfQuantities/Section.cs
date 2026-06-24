using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A grouping of line items inside a single Bill of Quantities (e.g. Foundation, Structure, Roof
/// within a "La Roșu" quote) — the internal structure of one quote, distinct from a Work Package.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ
/// but is never referenced from outside it, so the BoQ root owns its whole lifecycle and that of
/// its <see cref="LineItem"/>s and <see cref="Subsection"/>s. It carries the BoQ's pricing
/// <see cref="Currency"/> so its <see cref="Subtotal"/> is well-defined even when empty and so every
/// line shares one currency. Line items may sit <b>directly</b> in the section or be grouped one
/// level deeper inside a <see cref="Subsection"/> (the nesting depth is fixed at one); the
/// <see cref="Subtotal"/> sums both.
/// </remarks>
public sealed class Section : Entity<SectionId>
{
    private readonly List<LineItem> _lineItems = [];
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
    /// The line items held <b>directly</b> in this section, i.e. not inside a subsection (internal
    /// entities). Mutated only through the <see cref="BillOfQuantities"/> root; EF reaches the
    /// backing field directly.
    /// </summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>
    /// The subsections of this section (internal entities), an optional second level of grouping.
    /// Mutated only through the <see cref="BillOfQuantities"/> root; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<Subsection> Subsections => _subsections.AsReadOnly();

    /// <summary>
    /// Derived net subtotal (VAT-exclusive), in the pricing currency: the sum of the section's
    /// direct line totals plus every subsection's subtotal.
    /// </summary>
    public Money Subtotal =>
        _subsections.Aggregate(
            _lineItems.Aggregate(Money.Zero(Currency), (sum, item) => sum.Add(item.LineTotal)),
            (sum, sub) => sum.Add(sub.Subtotal));

    /// <summary>
    /// Derived gross subtotal (VAT-inclusive): the section's direct VAT-inclusive line totals plus
    /// every subsection's VAT-inclusive subtotal.
    /// </summary>
    public Money SubtotalWithVat =>
        _subsections.Aggregate(
            _lineItems.Aggregate(Money.Zero(Currency), (sum, item) => sum.Add(item.LineTotalWithVat)),
            (sum, sub) => sum.Add(sub.SubtotalWithVat));

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

    internal LineItem AddLineItem(
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        EnsureSharedCurrency(unitPrice);
        var item = new LineItem(LineItemId.New(), description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
        _lineItems.Add(item);
        return item;
    }

    internal bool ReviseLineItem(
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
        {
            return false;
        }

        EnsureSharedCurrency(unitPrice);
        item.Revise(description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
        return true;
    }

    internal bool RemoveLineItem(LineItemId lineItemId)
    {
        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
        {
            return false;
        }

        _lineItems.Remove(item);
        return true;
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

    internal LineItem? AddSubsectionLineItem(
        SubsectionId subsectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes) =>
        FindSubsection(subsectionId)?.AddLineItem(description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);

    internal bool ReviseSubsectionLineItem(
        SubsectionId subsectionId,
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        var subsection = FindSubsection(subsectionId);
        return subsection is not null
            && subsection.ReviseLineItem(lineItemId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    internal bool RemoveSubsectionLineItem(SubsectionId subsectionId, LineItemId lineItemId)
    {
        var subsection = FindSubsection(subsectionId);
        return subsection is not null && subsection.RemoveLineItem(lineItemId);
    }

    // — Reordering of the section's directly-held lines (driven by the BoQ root's MoveLineItem). —
    // The section's direct list and each subsection are the line-item "containers" the root addresses.

    /// <summary>Whether a line is held <b>directly</b> in this section (not inside a subsection).</summary>
    internal bool ContainsLineItem(LineItemId lineItemId) => LineItemOrdering.Contains(_lineItems, lineItemId);

    /// <summary>Detach a directly-held line for a move to another container (renumbers the remainder).</summary>
    internal LineItem DetachLineItem(LineItemId lineItemId) => LineItemOrdering.Detach(_lineItems, lineItemId);

    /// <summary>Insert a line (copy, same id) into the section's direct list at an index, then renumber.</summary>
    internal void InsertLineItem(LineItem source, int index) => LineItemOrdering.InsertCopy(_lineItems, source, index, Currency);

    /// <summary>Reorder a directly-held line within the section to an index, then renumber.</summary>
    internal void MoveLineItemWithin(LineItemId lineItemId, int index) => LineItemOrdering.MoveWithin(_lineItems, lineItemId, index);

    internal Subsection? FindSubsection(SubsectionId subsectionId) =>
        _subsections.FirstOrDefault(s => s.Id == subsectionId);

    private void EnsureSharedCurrency(Money unitPrice)
    {
        if (unitPrice.Currency != Currency)
        {
            throw new DomainValidationException(
                $"Line item price currency ({unitPrice.Currency}) must match the bill's pricing currency ({Currency}).",
                code: "LineItemCurrencyMismatch",
                parameters: new Dictionary<string, object?>
                {
                    ["lineCurrency"] = unitPrice.Currency.ToString(),
                    ["billCurrency"] = Currency.ToString(),
                });
        }
    }

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
