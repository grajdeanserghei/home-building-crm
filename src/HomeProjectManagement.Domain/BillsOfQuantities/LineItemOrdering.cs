using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// Ordering operations shared by the two kinds of line-item container — a <see cref="Section"/>'s
/// directly-held lines and a <see cref="Subsection"/>'s lines. Both keep their items in a
/// <c>List&lt;LineItem&gt;</c>; these helpers reorder, detach and (re)insert within such a list and
/// keep its <see cref="LineItem.Sequence"/> dense and contiguous (1..N), driven by the
/// <see cref="BillOfQuantities"/> root's <c>MoveLineItem</c>.
/// </summary>
/// <remarks>
/// A move <b>across</b> containers is modelled as detach-then-insert-a-copy preserving the
/// <see cref="LineItemId"/>: the two containers are mapped to different owned tables, so re-creating
/// the line under the new owner is an unambiguous delete+insert for EF rather than re-parenting a
/// tracked owned entity. A move <b>within</b> a container keeps the same instance (only its sequence
/// changes), so EF sees a plain update.
/// </remarks>
internal static class LineItemOrdering
{
    internal static bool Contains(List<LineItem> items, LineItemId id) =>
        items.Any(li => li.Id == id);

    /// <summary>Remove the line from this list and renumber the remainder; returns the removed instance.</summary>
    internal static LineItem Detach(List<LineItem> items, LineItemId id)
    {
        var item = items.FirstOrDefault(li => li.Id == id)
            ?? throw new DomainValidationException("The line item is not in this container.", code: "BoqLineItemNotFound");

        items.Remove(item);
        Renumber(items);
        return item;
    }

    /// <summary>
    /// Insert a <b>copy</b> of <paramref name="source"/> (same <see cref="LineItemId"/> and data) at
    /// <paramref name="index"/> (clamped), then renumber. Used when moving a line into this container
    /// from another one; the copy makes the change an unambiguous insert for EF.
    /// </summary>
    internal static void InsertCopy(List<LineItem> items, LineItem source, int index, Currency currency)
    {
        EnsureSharedCurrency(source.UnitPrice, currency);

        // Sequence is assigned by Renumber below; pass a placeholder.
        var copy = new LineItem(
            source.Id,
            source.Description,
            source.Quantity,
            source.UnitOfMeasureId,
            source.UnitPrice,
            source.VatRate,
            0,
            source.Notes);

        items.Insert(Math.Clamp(index, 0, items.Count), copy);
        Renumber(items);
    }

    /// <summary>Reorder an existing line within the same list to <paramref name="index"/> (clamped), then renumber.</summary>
    internal static void MoveWithin(List<LineItem> items, LineItemId id, int index)
    {
        var item = items.FirstOrDefault(li => li.Id == id)
            ?? throw new DomainValidationException("The line item is not in this container.", code: "BoqLineItemNotFound");

        items.Remove(item);
        items.Insert(Math.Clamp(index, 0, items.Count), item);
        Renumber(items);
    }

    /// <summary>Stamp a dense, contiguous 1..N <see cref="LineItem.Sequence"/> following list order.</summary>
    internal static void Renumber(List<LineItem> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            items[i].Resequence(i + 1);
        }
    }

    private static void EnsureSharedCurrency(Money unitPrice, Currency currency)
    {
        if (unitPrice.Currency != currency)
        {
            throw new DomainValidationException(
                $"Line item price currency ({unitPrice.Currency}) must match the bill's pricing currency ({currency}).",
                code: "LineItemCurrencyMismatch",
                parameters: new Dictionary<string, object?>
                {
                    ["lineCurrency"] = unitPrice.Currency.ToString(),
                    ["billCurrency"] = currency.ToString(),
                });
        }
    }
}
