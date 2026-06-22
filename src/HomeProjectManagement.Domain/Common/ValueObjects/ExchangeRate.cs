namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A pinned conversion rate (e.g. 1 EUR = 4.97 RON, as of a date). A Bill of Quantities
/// stores amounts in one pricing currency and pins one rate so the other currency is
/// derivable — a single source of truth rather than two independently-entered amounts.
/// </summary>
public sealed class ExchangeRate : ValueObject
{
    public Currency BaseCurrency { get; }
    public Currency QuoteCurrency { get; }
    public decimal Rate { get; }
    public DateOnly AsOf { get; }

    public ExchangeRate(Currency baseCurrency, Currency quoteCurrency, decimal rate, DateOnly asOf)
    {
        if (rate <= 0m)
        {
            throw new DomainValidationException("Exchange rate must be positive.", nameof(rate));
        }

        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        Rate = rate;
        AsOf = asOf;
    }

    /// <summary>Convert an amount in the base currency to the quote currency using this rate.</summary>
    public Money Convert(Money amount)
    {
        if (amount.Currency != BaseCurrency)
        {
            throw new InvalidOperationException(
                $"Cannot convert {amount.Currency}; this rate's base currency is {BaseCurrency}.");
        }

        return new Money(amount.Amount * Rate, QuoteCurrency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BaseCurrency;
        yield return QuoteCurrency;
        yield return Rate;
        yield return AsOf;
    }
}
