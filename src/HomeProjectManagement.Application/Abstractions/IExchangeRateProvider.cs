using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.Abstractions;

/// <summary>
/// Driven port for EUR↔RON rates. Initially backed by manual entry (the rate is captured
/// per Bill of Quantities); a BNR feed adapter can replace it later behind this port.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>The rate to use as of the given date, converting from base to quote currency.</summary>
    ExchangeRate GetRate(Currency baseCurrency, Currency quoteCurrency, DateOnly asOf);
}
