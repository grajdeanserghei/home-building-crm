using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Infrastructure.Events;
using HomeProjectManagement.Infrastructure.ExchangeRates;
using HomeProjectManagement.Infrastructure.Identity;
using HomeProjectManagement.Infrastructure.Persistence;
using HomeProjectManagement.Infrastructure.Persistence.Repositories;
using HomeProjectManagement.Infrastructure.Time;
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

        // Cross-cutting driven ports.
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, StubCurrentUser>();
        services.AddSingleton<IExchangeRateProvider, ManualExchangeRateProvider>();
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        return services;
    }
}
