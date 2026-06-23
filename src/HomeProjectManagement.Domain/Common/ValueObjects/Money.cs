using System.Globalization;

namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A monetary amount in a single <see cref="Currency"/>. Immutable; arithmetic across
/// different currencies is rejected — convert explicitly via an <see cref="ExchangeRate"/>.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on Money of differing currencies ({Currency} vs {other.Currency}).");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    // Pinned to the invariant culture so any output that reaches persistence, logs or the wire
    // is stable regardless of the host's ambient thread culture (always a dot decimal separator,
    // never a comma). Human-facing money is formatted on the frontend with Intl.NumberFormat('ro-RO').
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.##} {Currency}");
}
