using HomeProjectManagement.Domain.BillsOfQuantities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="BillOfQuantities"/> aggregate root. The root owns its
/// <see cref="Section"/> headings (each owning its <see cref="Subsection"/> headings) and, in a single
/// flat child table, every <see cref="LineItem"/> — each carrying the section (and optional
/// subsection) it is grouped under as plain id columns. Keeping the lines in one table makes moving a
/// line between containers a column update rather than a delete+insert across owned tables. Each line
/// flattens its <c>Money</c> unit price into amount + currency columns. Derived totals (BoQ total,
/// section/subsection subtotal, line total) are computed in the domain and never stored.
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
        // At most one BoQ per bid — enforced by a unique index on the owning bid.
        builder.HasIndex(b => b.BidId).IsUnique();

        builder.Property(b => b.Reference).HasMaxLength(100);

        // Persist the enums as their string names.
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(b => b.PricingCurrency).HasConversion<string>().HasMaxLength(3).IsRequired();
        // What the supplier priced against (entire building / per apartment). Existing rows backfill to
        // the whole building via the migration's column default.
        builder.Property(b => b.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(b => b.SubmittedOn);
        builder.Property(b => b.ValidUntil);

        // Provenance + idempotency for document ingestion. The hash is the SHA-256 the agent computed
        // over the source deviz; indexed (non-unique) so the application service can find a prior
        // ingestion of the same document and avoid duplicating it.
        builder.Property(b => b.SourceContentHash).HasMaxLength(64);
        builder.HasIndex(b => b.SourceContentHash);

        // Optional source-document reference, flattened into nullable columns.
        builder.OwnsOne(b => b.SourceDocument, doc =>
        {
            doc.Property(d => d.FileName).HasColumnName("source_document_file_name").HasMaxLength(400);
            doc.Property(d => d.Url).HasColumnName("source_document_url").HasMaxLength(2000);
            doc.Property(d => d.UploadedOn).HasColumnName("source_document_uploaded_on");
            doc.Property(d => d.UploadedBy).HasColumnName("source_document_uploaded_by");
        });

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

        // The totals are derived from the line totals; never stored.
        builder.Ignore(b => b.Total);
        builder.Ignore(b => b.TotalWithVat);

        // Sections are internal heading entities owned by the BoQ; each section optionally owns a
        // second level of subsection headings. Neither owns line items — those are held flat (below).
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

            sections.HasIndex("BoqId");

            // Subsections: an optional second level of grouping headings.
            sections.OwnsMany(s => s.Subsections, subsections =>
            {
                subsections.ToTable("boq_subsections");

                subsections.WithOwner().HasForeignKey("SectionId");
                subsections.HasKey(s => s.Id);
                subsections.Property(s => s.Id).ValueGeneratedNever();

                subsections.Property(s => s.Name).HasMaxLength(200).IsRequired();
                subsections.Property(s => s.Sequence).IsRequired();
                subsections.Property(s => s.Description).HasMaxLength(1000);
                subsections.Property(s => s.Currency).HasConversion<string>().HasMaxLength(3).IsRequired();

                subsections.HasIndex("SectionId");
            });

            sections.Navigation(s => s.Subsections)
                .HasField("_subsections")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // The sections collection is mutated only through the aggregate; EF reaches the backing field.
        builder.Navigation(b => b.Sections)
            .HasField("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // All line items live in one flat table owned directly by the BoQ. Each line carries the
        // section it belongs to (always) and the subsection it sits in (null when held directly in the
        // section) as plain id columns, so re-grouping a line is a column update on the same row.
        builder.OwnsMany(b => b.LineItems, items =>
        {
            items.ToTable("boq_line_items");

            items.WithOwner().HasForeignKey("BoqId");
            items.HasKey(li => li.Id);
            items.Property(li => li.Id).ValueGeneratedNever();

            // Grouping carried by id columns (mapped to Guid via the strongly-typed id convention).
            items.Property(li => li.SectionId).IsRequired();
            items.Property(li => li.SubsectionId);

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

            items.HasIndex("BoqId");
            items.HasIndex(li => li.SectionId);
            items.HasIndex(li => li.SubsectionId);
        });

        builder.Navigation(b => b.LineItems)
            .HasField("_lineItems")
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
