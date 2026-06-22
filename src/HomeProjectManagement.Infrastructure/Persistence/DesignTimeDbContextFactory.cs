using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeProjectManagement.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (e.g. <c>dotnet ef migrations add</c>). At runtime the
/// DbContext is configured by Aspire via <c>AddNpgsqlDbContext</c> in ApiService, so the
/// connection string below is a design-time placeholder and is never used to connect.
/// Migrations live in this (Infrastructure) assembly, beside the DbContext.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=projectsdb;Username=postgres;Password=postgres")
            .Options;

        return new AppDbContext(options);
    }
}
