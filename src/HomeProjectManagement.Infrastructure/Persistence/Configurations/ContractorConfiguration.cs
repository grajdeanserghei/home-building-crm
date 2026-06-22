using HomeProjectManagement.Domain.Contractors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="Contractor"/> aggregate root.</summary>
public sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("contractors");

        builder.HasKey(c => c.Id);
        // Ids are generated in the domain (ContractorId.New()), never by the database.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.FiscalCode).HasMaxLength(32);
        builder.Property(c => c.RegistrationNumber).HasMaxLength(32);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        // Master-data directory is browsed and searched by name.
        builder.HasIndex(c => c.Name);

        // Contact person is an optional owned value object (flattened into nullable columns).
        builder.OwnsOne(c => c.Contact, contact =>
        {
            contact.Property(ci => ci.PersonName).HasColumnName("contact_person_name").HasMaxLength(200);
            contact.Property(ci => ci.Email).HasColumnName("contact_email").HasMaxLength(320);
            contact.Property(ci => ci.Phone).HasColumnName("contact_phone").HasMaxLength(50);
        });

        // Address is an optional owned value object (flattened into nullable columns).
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100);
            address.Property(a => a.County).HasColumnName("address_county").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("address_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(100);
        });

        // Audit fields stamped by the unit of work.
        builder.Property(c => c.CreatedOn).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.ModifiedOn).IsRequired();
        builder.Property(c => c.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(c => c.DomainEvents);
    }
}
