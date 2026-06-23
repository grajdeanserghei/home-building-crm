using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.Trades;
using HomeProjectManagement.Domain.UnitsOfMeasure;
using HomeProjectManagement.Domain.WorkPackages;
using HomeProjectManagement.Infrastructure.Events;
using HomeProjectManagement.Infrastructure.ExchangeRates;
using HomeProjectManagement.Infrastructure.Identity;
using HomeProjectManagement.Infrastructure.Persistence;
using HomeProjectManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HomeProjectManagement.Infrastructure;

/// <summary>
/// Registers the driven adapters that implement the domain/application ports. The
/// <see cref="Persistence.AppDbContext"/> itself is registered by the host (ApiService)
/// through Aspire's <c>AddNpgsqlDbContext</c>, which owns the connection to <c>projectsdb</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Persistence ports.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkPackageRepository, WorkPackageRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<IBillOfQuantitiesRepository, BillOfQuantitiesRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();

        // Cross-cutting driven ports. TimeProvider is the BCL's native clock abstraction
        // (FakeTimeProvider is its test double); no custom IClock/SystemClock needed.
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, StubCurrentUser>();
        services.AddSingleton<IExchangeRateProvider, ManualExchangeRateProvider>();
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        return services;
    }
}
