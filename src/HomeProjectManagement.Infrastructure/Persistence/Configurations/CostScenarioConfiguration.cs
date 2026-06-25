using HomeProjectManagement.Domain.CostScenarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="CostScenario"/> aggregate root.</summary>
public sealed class CostScenarioConfiguration : IEntityTypeConfiguration<CostScenario>
{
    public void Configure(EntityTypeBuilder<CostScenario> builder)
    {
        builder.ToTable("cost_scenarios");

        builder.HasKey(s => s.Id);
        // Ids are generated in the domain (CostScenarioId.New()), never by the database.
        builder.Property(s => s.Id).ValueGeneratedNever();

        // The owning project is held by id (Guid column via the strongly-typed id convention),
        // not as an EF navigation — aggregates never load one another.
        builder.Property(s => s.ProjectId).IsRequired();
        builder.HasIndex(s => s.ProjectId);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);

        // Selections are internal entities owned by the scenario: one row per chosen bid, living and
        // dying with the scenario and loaded with it.
        builder.OwnsMany(s => s.Selections, selections =>
        {
            selections.ToTable("cost_scenario_selections");

            selections.WithOwner().HasForeignKey("CostScenarioId");
            // Composite key (owner + work package) enforces the "one bid per work package" invariant
            // at the database level too.
            selections.HasKey("CostScenarioId", "WorkPackageId");
            selections.Property(x => x.WorkPackageId).HasColumnName("WorkPackageId");
            selections.Property(x => x.BidId).HasColumnName("BidId");
        });

        // The selections collection is mutated only through the aggregate; EF reaches the backing field.
        builder.Navigation(s => s.Selections)
            .HasField("_selections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit fields stamped by the unit of work.
        builder.Property(s => s.CreatedOn).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.ModifiedOn).IsRequired();
        builder.Property(s => s.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(s => s.DomainEvents);
    }
}
