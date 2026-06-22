using HomeProjectManagement.Domain.WorkPackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="WorkPackage"/> aggregate root.</summary>
public sealed class WorkPackageConfiguration : IEntityTypeConfiguration<WorkPackage>
{
    public void Configure(EntityTypeBuilder<WorkPackage> builder)
    {
        builder.ToTable("work_packages");

        builder.HasKey(wp => wp.Id);
        // Ids are generated in the domain (WorkPackageId.New()), never by the database.
        builder.Property(wp => wp.Id).ValueGeneratedNever();

        // References to other aggregates are held by id (Guid columns via the strongly-typed
        // id convention), not as EF navigations — aggregates never load one another.
        builder.Property(wp => wp.ProjectId).IsRequired();
        builder.Property(wp => wp.AwardedContractId);

        // Find/order a project's packages efficiently.
        builder.HasIndex(wp => wp.ProjectId);

        builder.Property(wp => wp.Name).HasMaxLength(200).IsRequired();
        builder.Property(wp => wp.Description).HasMaxLength(2000);

        // Persist the enum as its string name.
        builder.Property(wp => wp.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(wp => wp.Sequence).IsRequired();
        builder.Property(wp => wp.PlannedStartDate);
        builder.Property(wp => wp.PlannedEndDate);

        // Scope items are internal entities owned by the work package: a child table whose rows live
        // and die with the package and are loaded with it.
        builder.OwnsMany(wp => wp.ScopeItems, scopeItems =>
        {
            scopeItems.ToTable("work_package_scope_items");

            scopeItems.WithOwner().HasForeignKey("WorkPackageId");
            scopeItems.HasKey(si => si.Id);
            scopeItems.Property(si => si.Id).ValueGeneratedNever();

            scopeItems.Property(si => si.Name).HasMaxLength(200).IsRequired();
            scopeItems.Property(si => si.Description).HasMaxLength(2000);
            scopeItems.Property(si => si.Requirement).HasConversion<string>().HasMaxLength(16).IsRequired();
            scopeItems.Property(si => si.Sequence).IsRequired();

            // Name unique within the work package (the scope item's headline invariant).
            scopeItems.HasIndex("WorkPackageId", "Name").IsUnique();
        });

        // The scope items collection is mutated only through the aggregate; EF reaches the backing field.
        builder.Navigation(wp => wp.ScopeItems)
            .HasField("_scopeItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit fields stamped by the unit of work.
        builder.Property(wp => wp.CreatedOn).IsRequired();
        builder.Property(wp => wp.CreatedBy).IsRequired();
        builder.Property(wp => wp.ModifiedOn).IsRequired();
        builder.Property(wp => wp.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(wp => wp.DomainEvents);
    }
}
