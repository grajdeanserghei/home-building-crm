using HomeProjectManagement.Domain.ConstructionValuations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="ConstructionValuation"/> aggregate root — a dated, frozen snapshot.
/// The root owns a flat list of <see cref="ConstructionValuationItem"/>s whose money fields were computed
/// once at capture and are stored verbatim (never derived on read). The pinned exchange rate and optional
/// source document are flattened owned value objects. The source content hash is indexed to back idempotent
/// import.
/// </summary>
public sealed class ConstructionValuationConfiguration : IEntityTypeConfiguration<ConstructionValuation>
{
    public void Configure(EntityTypeBuilder<ConstructionValuation> builder)
    {
        builder.ToTable("construction_valuations");

        builder.HasKey(v => v.Id);
        // Ids are generated in the domain (ConstructionValuationId.New()), never by the database.
        builder.Property(v => v.Id).ValueGeneratedNever();

        // The assessed catalog is held by id, not as an EF navigation. Many snapshots per catalog.
        builder.Property(v => v.ValuationCatalogId).IsRequired();
        builder.HasIndex(v => v.ValuationCatalogId);

        builder.Property(v => v.AssessedOn).IsRequired();
        builder.Property(v => v.Appraiser).HasMaxLength(200);

        // Content hash for idempotent import; indexed (non-unique) so the app service finds a prior import.
        builder.Property(v => v.SourceContentHash).HasMaxLength(64);
        builder.HasIndex(v => v.SourceContentHash);

        // Pinned RON/EUR rate (required on a snapshot).
        builder.OwnsOne(v => v.ExchangeRate, rate =>
        {
            rate.Property(r => r.BaseCurrency)
                .HasColumnName("exchange_rate_base_currency").HasConversion<string>().HasMaxLength(3);
            rate.Property(r => r.QuoteCurrency)
                .HasColumnName("exchange_rate_quote_currency").HasConversion<string>().HasMaxLength(3);
            rate.Property(r => r.Rate).HasColumnName("exchange_rate_rate").HasPrecision(18, 6);
            rate.Property(r => r.AsOf).HasColumnName("exchange_rate_as_of");
        });
        builder.Navigation(v => v.ExchangeRate).IsRequired();

        // Optional source-document reference, flattened into nullable columns.
        builder.OwnsOne(v => v.SourceDocument, doc =>
        {
            doc.Property(d => d.FileName).HasColumnName("source_document_file_name").HasMaxLength(400);
            doc.Property(d => d.Url).HasColumnName("source_document_url").HasMaxLength(2000);
            doc.Property(d => d.UploadedOn).HasColumnName("source_document_uploaded_on");
            doc.Property(d => d.UploadedBy).HasColumnName("source_document_uploaded_by");
        });

        // Frozen assessed rows: every money field stored verbatim.
        builder.OwnsMany(v => v.Items, items =>
        {
            items.ToTable("construction_valuation_items");

            items.WithOwner().HasForeignKey("ConstructionValuationId");
            items.HasKey(i => i.Id);
            items.Property(i => i.Id).ValueGeneratedNever();

            items.Property(i => i.ValuationCatalogItemId).IsRequired();
            items.Property(i => i.Name).HasMaxLength(400).IsRequired();
            items.Property(i => i.CompletionPercentage).HasPrecision(6, 3);
            items.Property(i => i.RemainingPercentage).HasPrecision(6, 3);

            items.OwnsOne(i => i.EstimatedValueWithoutVat, m => Money(m, "estimated_without_vat"));
            items.Navigation(i => i.EstimatedValueWithoutVat).IsRequired();

            items.OwnsOne(i => i.EstimatedValueWithVat, m => Money(m, "estimated_with_vat"));
            items.Navigation(i => i.EstimatedValueWithVat).IsRequired();

            items.OwnsOne(i => i.CompletedValueWithoutVat, m => Money(m, "completed_without_vat"));
            items.Navigation(i => i.CompletedValueWithoutVat).IsRequired();

            items.OwnsOne(i => i.CompletedValueWithVat, m => Money(m, "completed_with_vat"));
            items.Navigation(i => i.CompletedValueWithVat).IsRequired();

            items.OwnsOne(i => i.RemainingValueWithoutVat, m => Money(m, "remaining_without_vat"));
            items.Navigation(i => i.RemainingValueWithoutVat).IsRequired();

            items.OwnsOne(i => i.RemainingValueWithVat, m => Money(m, "remaining_with_vat"));
            items.Navigation(i => i.RemainingValueWithVat).IsRequired();

            items.HasIndex("ConstructionValuationId");
        });

        builder.Navigation(v => v.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit fields stamped by the unit of work.
        builder.Property(v => v.CreatedOn).IsRequired();
        builder.Property(v => v.CreatedBy).IsRequired();
        builder.Property(v => v.ModifiedOn).IsRequired();
        builder.Property(v => v.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(v => v.DomainEvents);
    }

    // Flatten a frozen Money value object into amount + currency columns with a shared column-name prefix.
    private static void Money(
        OwnedNavigationBuilder<ConstructionValuationItem, Domain.Common.ValueObjects.Money> money,
        string prefix)
    {
        money.Property(p => p.Amount).HasColumnName($"{prefix}_amount").HasPrecision(18, 2);
        money.Property(p => p.Currency).HasColumnName($"{prefix}_currency").HasConversion<string>().HasMaxLength(3);
    }
}
