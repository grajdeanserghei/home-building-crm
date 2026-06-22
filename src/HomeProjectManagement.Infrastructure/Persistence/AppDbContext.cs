using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.UnitsOfMeasure;
using HomeProjectManagement.Domain.WorkPackages;
using HomeProjectManagement.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence;

/// <summary>
/// The EF Core unit of persistence. Exposes a <see cref="DbSet{T}"/> per aggregate root
/// only; internal entities are reached through their root. Mappings live in one
/// <c>IEntityTypeConfiguration</c> per aggregate under <c>Configurations/</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkPackage> WorkPackages => Set<WorkPackage>();

    public DbSet<Contractor> Contractors => Set<Contractor>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ApplyStronglyTypedIdConversions();
    }
}
