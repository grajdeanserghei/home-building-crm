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

    public override string ToString() => $"{Amount:0.##} {Currency}";
}
