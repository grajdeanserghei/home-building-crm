using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter exposing the single app-wide EUR↔RON <i>display</i> rate to the
/// frontend. The global currency toggle (EUR / RON / Original) converts figures on pages whose own
/// DTOs carry no rate, so it needs one lightweight place to read the current "1 EUR = N RON" value.
/// This is the approximate display rate from <see cref="IExchangeRateProvider"/>; the pinned per-BoQ
/// rate remains the source of truth for a specific quote.
/// </summary>
public static class ExchangeRateEndpoints
{
    /// <summary>The app-wide display rate, "1 EUR = <paramref name="RonPerEur"/> RON".</summary>
    public record ExchangeRateDto(decimal RonPerEur);

    public static IEndpointRouteBuilder MapExchangeRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exchange-rate",
            (IExchangeRateProvider exchangeRates, TimeProvider timeProvider) =>
            {
                var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
                var ronPerEur = exchangeRates.GetRate(Currency.EUR, Currency.RON, asOf).Rate;
                return Results.Ok(new ExchangeRateDto(ronPerEur));
            });

        return app;
    }
}
