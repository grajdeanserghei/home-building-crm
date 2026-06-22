using HomeProjectManagement.Application.Bids;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.Contractors;
using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Application.Projects;
using HomeProjectManagement.Application.UnitsOfMeasure;
using HomeProjectManagement.Application.WorkPackages;
using Microsoft.Extensions.DependencyInjection;

namespace HomeProjectManagement.Application;

/// <summary>Registers the application-layer use-case services (driving ports).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectAppService, ProjectAppService>();
        services.AddScoped<IWorkPackageAppService, WorkPackageAppService>();
        services.AddScoped<IContractorAppService, ContractorAppService>();
        services.AddScoped<IBidAppService, BidAppService>();
        services.AddScoped<IBillOfQuantitiesAppService, BillOfQuantitiesAppService>();
        services.AddScoped<IContractAppService, ContractAppService>();
        services.AddScoped<IUnitOfMeasureAppService, UnitOfMeasureAppService>();
        return services;
    }
}
