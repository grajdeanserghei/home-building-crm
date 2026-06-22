using HomeProjectManagement.Application.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace HomeProjectManagement.Application;

/// <summary>Registers the application-layer use-case services (driving ports).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectAppService, ProjectAppService>();
        return services;
    }
}
