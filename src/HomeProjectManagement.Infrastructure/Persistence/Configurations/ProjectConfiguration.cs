using HomeProjectManagement.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="Project"/> aggregate root.</summary>
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        // Ids are generated in the domain (ProjectId.New()), never by the database.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        // Persist the enum as its string name (matches the frontend's union type).
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(p => p.StartDate);
        builder.Property(p => p.TargetCompletionDate);

        // How many dwelling units the build has — the per-apartment cost multiplier. At least 1; the
        // DB default backfills existing projects to a single unit (changes nothing until raised).
        builder.Property(p => p.ApartmentUnits).IsRequired().HasDefaultValue(1);

        // Audit fields stamped by the unit of work.
        builder.Property(p => p.CreatedOn).IsRequired();
        builder.Property(p => p.CreatedBy).IsRequired();
        builder.Property(p => p.ModifiedOn).IsRequired();
        builder.Property(p => p.ModifiedBy).IsRequired();

        // Site address is an optional owned value object (flattened into nullable columns).
        builder.OwnsOne(p => p.SiteAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("site_street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("site_city").HasMaxLength(100);
            address.Property(a => a.County).HasColumnName("site_county").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("site_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("site_country").HasMaxLength(100);
        });

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(p => p.DomainEvents);
    }
}
