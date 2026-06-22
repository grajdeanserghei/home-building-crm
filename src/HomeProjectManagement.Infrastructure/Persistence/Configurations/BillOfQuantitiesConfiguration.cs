using HomeProjectManagement.Domain.BillsOfQuantities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="BillOfQuantities"/> aggregate root and its owned section /
/// line-item hierarchy. Sections are a child table; each section in turn owns its line items, and
/// each line item flattens its <c>Money</c> unit price into amount + currency columns. Derived
/// totals (BoQ total, section subtotal, line total) are computed in the domain and never stored.
/// </summary>
public sealed class BillOfQuantitiesConfiguration : IEntityTypeConfiguration<BillOfQuantities>
{
    public void Configure(EntityTypeBuilder<BillOfQuantities> builder)
    {
        builder.ToTable("bills_of_quantities");

        builder.HasKey(b => b.Id);
        // Ids are generated in the domain (BoqId.New()), never by the database.
        builder.Property(b => b.Id).ValueGeneratedNever();

        // The owning bid is held by id (Guid column via the strongly-typed id convention), not as
        // an EF navigation — aggregates never load one another.
        builder.Property(b => b.BidId).IsRequired();
        // A bid may hold several BoQ versions; each version number is unique within the bid.
        builder.HasIndex(b => new { b.BidId, b.Version }).IsUnique();

        builder.Property(b => b.Reference).HasMaxLength(100);
        builder.Property(b => b.Version).IsRequired();

        // Persist the enums as their string names.
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(b => b.PricingCurrency).HasConversion<string>().HasMaxLength(3).IsRequired();

        builder.Property(b => b.SubmittedOn);
        builder.Property(b => b.ValidUntil);

        // Pinned exchange rate is an optional owned value object (flattened into nullable columns).
        builder.OwnsOne(b => b.ExchangeRate, rate =>
        {
            rate.Property(r => r.BaseCurrency)
                .HasColumnName("exchange_rate_base_currency").HasConversion<string>().HasMaxLength(3);
            rate.Property(r => r.QuoteCurrency)
                .HasColumnName("exchange_rate_quote_currency").HasConversion<string>().HasMaxLength(3);
            rate.Property(r => r.Rate).HasColumnName("exchange_rate_rate").HasPrecision(18, 6);
            rate.Property(r => r.AsOf).HasColumnName("exchange_rate_as_of");
        });

        // The totals are derived from the section subtotals; never stored.
        builder.Ignore(b => b.Total);
        builder.Ignore(b => b.TotalWithVat);

        // Sections are internal entities owned by the BoQ; each section owns its line items in turn.
        builder.OwnsMany(b => b.Sections, sections =>
        {
            sections.ToTable("boq_sections");

            sections.WithOwner().HasForeignKey("BoqId");
            sections.HasKey(s => s.Id);
            sections.Property(s => s.Id).ValueGeneratedNever();

            sections.Property(s => s.Name).HasMaxLength(200).IsRequired();
            sections.Property(s => s.Sequence).IsRequired();
            sections.Property(s => s.Description).HasMaxLength(1000);
            sections.Property(s => s.Currency).HasConversion<string>().HasMaxLength(3).IsRequired();

            // Subtotals are derived from the line totals; never stored.
            sections.Ignore(s => s.Subtotal);
            sections.Ignore(s => s.SubtotalWithVat);

            sections.HasIndex("BoqId");

            sections.OwnsMany(s => s.LineItems, items =>
            {
                items.ToTable("boq_line_items");

                items.WithOwner().HasForeignKey("SectionId");
                items.HasKey(li => li.Id);
                items.Property(li => li.Id).ValueGeneratedNever();

                items.Property(li => li.Description).HasMaxLength(1000).IsRequired();
                items.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
                // The referenced canonical unit is held by id, not as a navigation.
                items.Property(li => li.UnitOfMeasureId).IsRequired();
                items.Property(li => li.Sequence).IsRequired();
                items.Property(li => li.Notes).HasMaxLength(1000);

                // Net/gross line totals and the gross unit price are derived; never stored.
                items.Ignore(li => li.LineTotal);
                items.Ignore(li => li.LineTotalWithVat);
                items.Ignore(li => li.UnitPriceWithVat);

                // Net unit price is an owned value object flattened into amount + currency columns.
                items.OwnsOne(li => li.UnitPrice, price =>
                {
                    price.Property(p => p.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2);
                    price.Property(p => p.Currency)
                        .HasColumnName("unit_price_currency").HasConversion<string>().HasMaxLength(3);
                });
                items.Navigation(li => li.UnitPrice).IsRequired();

                // VAT rate is an owned value object flattened into a single percentage column.
                items.OwnsOne(li => li.VatRate, vat =>
                {
                    vat.Property(v => v.Percentage).HasColumnName("vat_rate_percentage").HasPrecision(5, 2);
                });
                items.Navigation(li => li.VatRate).IsRequired();

                items.HasIndex("SectionId");
            });

            // Line items are mutated only through the aggregate; EF reaches the backing field.
            sections.Navigation(s => s.LineItems)
                .HasField("_lineItems")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // The sections collection is mutated only through the aggregate; EF reaches the backing field.
        builder.Navigation(b => b.Sections)
            .HasField("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit fields stamped by the unit of work.
        builder.Property(b => b.CreatedOn).IsRequired();
        builder.Property(b => b.CreatedBy).IsRequired();
        builder.Property(b => b.ModifiedOn).IsRequired();
        builder.Property(b => b.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(b => b.DomainEvents);
    }
}
