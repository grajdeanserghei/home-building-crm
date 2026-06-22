using HomeProjectManagement.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="Contract"/> aggregate root. References to the work package and
/// the accepted BoQ are held by id (Guid columns via the strongly-typed id convention), never as EF
/// navigations — aggregates never load one another. The agreed <c>Value</c> flattens into amount +
/// currency columns.
/// </summary>
public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(c => c.Id);
        // Ids are generated in the domain (ContractId.New()), never by the database.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.WorkPackageId).IsRequired();
        builder.Property(c => c.AcceptedBoqId).IsRequired();

        // At most one contract per work package (the aggregate's headline cross-instance invariant).
        builder.HasIndex(c => c.WorkPackageId).IsUnique();

        builder.Property(c => c.ContractNumber).HasMaxLength(100);

        // Persist the enum as its string name.
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        // Agreed value is an owned value object flattened into amount + currency columns.
        builder.OwnsOne(c => c.Value, value =>
        {
            value.Property(v => v.Amount).HasColumnName("value_amount").HasPrecision(18, 2);
            value.Property(v => v.Currency).HasColumnName("value_currency").HasConversion<string>().HasMaxLength(3);
        });
        builder.Navigation(c => c.Value).IsRequired();

        builder.Property(c => c.SignedOn);
        builder.Property(c => c.StartDate);
        builder.Property(c => c.PlannedEndDate);
        builder.Property(c => c.ActualEndDate);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        // Audit fields stamped by the unit of work.
        builder.Property(c => c.CreatedOn).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.ModifiedOn).IsRequired();
        builder.Property(c => c.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(c => c.DomainEvents);
    }
}
