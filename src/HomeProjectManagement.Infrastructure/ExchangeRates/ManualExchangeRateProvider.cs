using System.Globalization;
using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Infrastructure.ExchangeRates;

/// <summary>
/// Initial <see cref="IExchangeRateProvider"/>: the exact rate for a priced quote is still entered
/// manually per Bill of Quantities (pinned on the BoQ). Beyond that, this provider supplies a single
/// app-wide EUR↔RON <i>display</i> rate so cross-currency figures can be shown in a common currency
/// (e.g. the project budget's EUR-equivalent total). The rate defaults to
/// <see cref="DefaultRonPerEur"/> and can be overridden — without a rebuild — via the
/// <c>EXCHANGE_RATE_RON_PER_EUR</c> environment variable (Aspire can inject it). It is approximate by
/// nature; the pinned per-BoQ rate remains the source of truth for a specific quote. A BNR feed
/// adapter can replace this later behind the same port.
/// </summary>
public sealed class ManualExchangeRateProvider : IExchangeRateProvider
{
    /// <summary>Fallback "1 EUR = N RON" display rate when no override is configured.</summary>
    public const decimal DefaultRonPerEur = 5.07m;

    private readonly decimal _ronPerEur;

    public ManualExchangeRateProvider()
    {
        _ronPerEur = ReadConfiguredRonPerEur() ?? DefaultRonPerEur;
    }

    public ExchangeRate GetRate(Currency baseCurrency, Currency quoteCurrency, DateOnly asOf)
    {
        if (baseCurrency == quoteCurrency)
        {
            return new ExchangeRate(baseCurrency, quoteCurrency, 1m, asOf);
        }

        return (baseCurrency, quoteCurrency) switch
        {
            // 1 EUR = _ronPerEur RON.
            (Currency.EUR, Currency.RON) => new ExchangeRate(baseCurrency, quoteCurrency, _ronPerEur, asOf),
            // The inverse, so a RON amount can be expressed in EUR.
            (Currency.RON, Currency.EUR) => new ExchangeRate(baseCurrency, quoteCurrency, 1m / _ronPerEur, asOf),
            _ => throw new NotSupportedException(
                $"No exchange rate is configured from {baseCurrency} to {quoteCurrency}."),
        };
    }

    // Optional override, parsed culture-invariantly (a dot decimal separator) so it is stable
    // regardless of the host's ambient culture. A non-positive or unparseable value is ignored.
    private static decimal? ReadConfiguredRonPerEur()
    {
        var raw = Environment.GetEnvironmentVariable("EXCHANGE_RATE_RON_PER_EUR");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0m
            ? value
            : null;
    }
}
