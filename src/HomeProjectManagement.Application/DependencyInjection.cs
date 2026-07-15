using HomeProjectManagement.Application.Activity;
using HomeProjectManagement.Application.Bids;
using HomeProjectManagement.Application.BillsOfQuantities;
using HomeProjectManagement.Application.Budgeting;
using HomeProjectManagement.Application.Contractors;
using HomeProjectManagement.Application.Contracts;
using HomeProjectManagement.Application.CostScenarios;
using HomeProjectManagement.Application.Projects;
using HomeProjectManagement.Application.ConstructionValuations;
using HomeProjectManagement.Application.Trades;
using HomeProjectManagement.Application.UnitsOfMeasure;
using HomeProjectManagement.Application.ValuationCatalogs;
using HomeProjectManagement.Application.Valuations;
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
        services.AddScoped<ITradeAppService, TradeAppService>();
        services.AddScoped<IProjectBudgetQuery, ProjectBudgetQuery>();
        services.AddScoped<IProjectActivityQuery, ProjectActivityQuery>();
        services.AddScoped<ICostScenarioAppService, CostScenarioAppService>();
        services.AddScoped<ICostScenarioQuery, CostScenarioQuery>();
        services.AddScoped<IValuationCatalogAppService, ValuationCatalogAppService>();
        services.AddScoped<IConstructionValuationAppService, ConstructionValuationAppService>();
        services.AddScoped<IValuationVsBoqQuery, ValuationVsBoqQuery>();
        services.AddScoped<IValuationProgressQuery, ValuationProgressQuery>();
        return services;
    }
}
