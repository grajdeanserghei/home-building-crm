using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="Trade"/> aggregate root, seeded with the common trades.</summary>
public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades");

        builder.HasKey(t => t.Id);
        // Ids are generated in the domain (TradeId.New()), never by the database.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();   // unique canonical-name invariant

        builder.Property(t => t.Code).HasMaxLength(16);

        builder.Property(t => t.IsActive).IsRequired();

        // Audit fields stamped by the unit of work.
        builder.Property(t => t.CreatedOn).IsRequired();
        builder.Property(t => t.CreatedBy).IsRequired();
        builder.Property(t => t.ModifiedOn).IsRequired();
        builder.Property(t => t.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(t => t.DomainEvents);

        // Seed the common construction trades (the indicative list from the domain model). Inserted
        // by the migration, not the unit of work, so the audit fields are stamped here with a fixed
        // timestamp and the System user. Ids are fixed so the seed is stable across migrations.
        builder.HasData(SeedTrades());
    }

    private static IEnumerable<object> SeedTrades()
    {
        var seededOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new (string Id, string Name)[]
        {
            ("a1b2c3d4-0000-0000-0000-000000000001", "Zidărie"),
            ("a1b2c3d4-0000-0000-0000-000000000002", "Structură / Beton"),
            ("a1b2c3d4-0000-0000-0000-000000000003", "Instalații Electrice"),
            ("a1b2c3d4-0000-0000-0000-000000000004", "Instalații Sanitare"),
            ("a1b2c3d4-0000-0000-0000-000000000005", "Instalații Termice"),
            ("a1b2c3d4-0000-0000-0000-000000000006", "Instalații Răcire / Ventilare"),
            ("a1b2c3d4-0000-0000-0000-000000000007", "Tâmplărie"),
            ("a1b2c3d4-0000-0000-0000-000000000008", "Interioare / Finisaje"),
            ("a1b2c3d4-0000-0000-0000-000000000009", "Acoperiș / Învelitori"),
            ("a1b2c3d4-0000-0000-0000-000000000010", "Izolații"),
        }.Select(t => new
        {
            Id = new TradeId(Guid.Parse(t.Id)),
            Name = t.Name,
            Code = (string?)null,
            IsActive = true,
            CreatedOn = seededOn,
            CreatedBy = UserId.System,
            ModifiedOn = seededOn,
            ModifiedBy = UserId.System,
        });
    }
}
