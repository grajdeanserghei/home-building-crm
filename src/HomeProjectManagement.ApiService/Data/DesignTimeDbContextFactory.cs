using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeProjectManagement.ApiService.Data;

// Used only by the EF Core tools (e.g. `dotnet ef migrations add`).
// At runtime the DbContext is configured by Aspire via AddNpgsqlDbContext, so the
// connection string below is a design-time placeholder and is never used to connect.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=projectsdb;Username=postgres;Password=postgres")
            .Options;

        return new AppDbContext(options);
    }
}
