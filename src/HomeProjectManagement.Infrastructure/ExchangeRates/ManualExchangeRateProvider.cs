using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Infrastructure.ExchangeRates;

/// <summary>
/// Initial <see cref="IExchangeRateProvider"/>: rates are entered manually per Bill of
/// Quantities rather than fetched. Same-currency requests resolve to a 1:1 rate; a genuine
/// cross-currency rate must be supplied by the user when BoQ pricing lands. A BNR feed
/// adapter can replace this later behind the same port.
/// </summary>
public sealed class ManualExchangeRateProvider : IExchangeRateProvider
{
    public ExchangeRate GetRate(Currency baseCurrency, Currency quoteCurrency, DateOnly asOf)
    {
        if (baseCurrency == quoteCurrency)
        {
            return new ExchangeRate(baseCurrency, quoteCurrency, 1m, asOf);
        }

        throw new NotSupportedException(
            "Cross-currency rates are entered manually per Bill of Quantities; " +
            "no automatic rate source is configured yet.");
    }
}
