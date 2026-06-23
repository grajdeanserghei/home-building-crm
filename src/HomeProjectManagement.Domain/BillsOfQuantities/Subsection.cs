using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A fixed second-level grouping of <see cref="LineItem"/>s inside a <see cref="Section"/> (e.g.
/// "Excavation" and "Reinforcement" within a Foundation section). The nesting depth is fixed: a
/// subsection groups line items but never holds further subsections.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bill of Quantities aggregate</b>: it has identity within the BoQ
/// but is never referenced from outside it, so the BoQ root (via its owning <see cref="Section"/>)
/// owns its whole lifecycle and that of its <see cref="LineItem"/>s. Like a <see cref="Section"/>
/// it carries the BoQ's pricing <see cref="Currency"/> so its <see cref="Subtotal"/> is well-defined
/// even when empty and so every line shares one currency.
/// </remarks>
public sealed class Subsection : Entity<SubsectionId>
{
    private readonly List<LineItem> _lineItems = [];

    /// <summary>The subsection heading (e.g. "Excavation").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Order within the parent section.</summary>
    public int Sequence { get; private set; }

    /// <summary>Optional notes about the subsection.</summary>
    public string? Description { get; private set; }

    /// <summary>The BoQ's pricing currency; every line item in the subsection shares it.</summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// The line items in this subsection (internal entities). Mutated only through the
    /// <see cref="BillOfQuantities"/> root; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Derived net subtotal (VAT-exclusive): the sum of the subsection's line totals, in the pricing currency.</summary>
    public Money Subtotal =>
        _lineItems.Aggregate(Money.Zero(Currency), (sum, item) => sum.Add(item.LineTotal));

    /// <summary>Derived gross subtotal (VAT-inclusive): the sum of the subsection's VAT-inclusive line totals.</summary>
    public Money SubtotalWithVat =>
        _lineItems.Aggregate(Money.Zero(Currency), (sum, item) => sum.Add(item.LineTotalWithVat));

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
            throw new DomainValidationException("Subsection name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
