using HomeProjectManagement.Domain.UnitsOfMeasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="UnitOfMeasure"/> aggregate root.</summary>
public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("units_of_measure");

        builder.HasKey(u => u.Id);
        // Ids are generated in the domain (UnitOfMeasureId.New()), never by the database.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Code).HasMaxLength(16).IsRequired();
        // Canonical code is globally unique — the controlled-vocabulary invariant.
        builder.HasIndex(u => u.Code).IsUnique();

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();

        // Persist the enum as its string name.
        builder.Property(u => u.Category).HasConversion<string>().HasMaxLength(32).IsRequired();

        // Aliases are a primitive collection (mapped to a Postgres text[]). They are mutated
        // only through the aggregate, so EF reaches them via the encapsulated backing field.
        builder.PrimitiveCollection(u => u.Aliases)
            .HasField("_aliases")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.IsActive).IsRequired();

        // Audit fields stamped by the unit of work.
        builder.Property(u => u.CreatedOn).IsRequired();
        builder.Property(u => u.CreatedBy).IsRequired();
        builder.Property(u => u.ModifiedOn).IsRequired();
        builder.Property(u => u.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(u => u.DomainEvents);
    }
}
