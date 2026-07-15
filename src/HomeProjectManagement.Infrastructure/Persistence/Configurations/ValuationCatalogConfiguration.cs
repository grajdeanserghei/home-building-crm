using HomeProjectManagement.Domain.ValuationCatalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="ValuationCatalog"/> aggregate root. The root owns a flat list of
/// <see cref="ValuationCatalogItem"/>s, each owning its <see cref="ValuationItemLink"/> value objects (the
/// BoQ mapping). Money value objects are flattened into amount + currency columns; the VAT rate into a
/// single percentage column. One catalog per project is backed by a unique index on <c>ProjectId</c>. The
/// no-double-count invariant is backed by a unique index over the link tuple (a triple physically belongs to
/// one BoQ, hence one project, hence one catalog); the whole-section duplicate case (null subsection) is
/// caught by the aggregate root before it can reach the database.
/// </summary>
public sealed class ValuationCatalogConfiguration : IEntityTypeConfiguration<ValuationCatalog>
{
    public void Configure(EntityTypeBuilder<ValuationCatalog> builder)
    {
        builder.ToTable("valuation_catalogs");

        builder.HasKey(c => c.Id);
        // Ids are generated in the domain (ValuationCatalogId.New()), never by the database.
        builder.Property(c => c.Id).ValueGeneratedNever();

        // The owning project is held by id (Guid column via the strongly-typed id convention), not as an
        // EF navigation. One catalog per project — enforced by a unique index on the project.
        builder.Property(c => c.ProjectId).IsRequired();
        builder.HasIndex(c => c.ProjectId).IsUnique();

        builder.Property(c => c.CatalogReference).HasMaxLength(200).IsRequired();

        // Enums persisted as their string names.
        builder.Property(c => c.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.Currency).HasConversion<string>().HasMaxLength(3).IsRequired();

        builder.Property(c => c.BuiltArea).HasPrecision(18, 4);
        builder.Property(c => c.GrossFloorArea).HasPrecision(18, 4);
        builder.Property(c => c.UsableArea).HasPrecision(18, 4);
        builder.Property(c => c.OwnRegieAdjustment).HasPrecision(9, 4);

        // Report VAT rate is an owned value object flattened into a single percentage column.
        builder.OwnsOne(c => c.VatRate, vat =>
        {
            vat.Property(v => v.Percentage).HasColumnName("vat_rate_percentage").HasPrecision(5, 2);
        });
        builder.Navigation(c => c.VatRate).IsRequired();

        // Priced items are internal entities owned by the catalog; each owns its BoQ mappings.
        builder.OwnsMany(c => c.Items, items =>
        {
            items.ToTable("valuation_catalog_items");

            items.WithOwner().HasForeignKey("ValuationCatalogId");
            items.HasKey(i => i.Id);
            items.Property(i => i.Id).ValueGeneratedNever();

            items.Property(i => i.Sequence).IsRequired();
            items.Property(i => i.PrintedNumber).HasMaxLength(50).IsRequired();
            items.Property(i => i.Name).HasMaxLength(400).IsRequired();
            items.Property(i => i.Unit).HasMaxLength(50).IsRequired();
            items.Property(i => i.CatalogSource).HasMaxLength(50).IsRequired();
            items.Property(i => i.CostWeight).HasPrecision(9, 6);
            items.Property(i => i.IsActive).IsRequired();

            items.OwnsOne(i => i.UnitCostPerBuiltArea, m =>
            {
                m.Property(p => p.Amount).HasColumnName("unit_cost_amount").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("unit_cost_currency").HasConversion<string>().HasMaxLength(3);
            });
            items.Navigation(i => i.UnitCostPerBuiltArea).IsRequired();

            items.OwnsOne(i => i.TotalCostWithoutVat, m =>
            {
                m.Property(p => p.Amount).HasColumnName("total_cost_without_vat_amount").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("total_cost_without_vat_currency").HasConversion<string>().HasMaxLength(3);
            });
            items.Navigation(i => i.TotalCostWithoutVat).IsRequired();

            items.OwnsOne(i => i.TotalCostWithVat, m =>
            {
                m.Property(p => p.Amount).HasColumnName("total_cost_with_vat_amount").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("total_cost_with_vat_currency").HasConversion<string>().HasMaxLength(3);
            });
            items.Navigation(i => i.TotalCostWithVat).IsRequired();

            items.HasIndex("ValuationCatalogId");

            // BoQ mappings: value objects owned by the item, holding the mapped section/subsection by raw
            // id. A subsection link always carries its real parent SectionId (the app service populates it).
            items.OwnsMany(i => i.Links, links =>
            {
                links.ToTable("valuation_item_links");

                links.WithOwner().HasForeignKey("ValuationCatalogItemId");

                links.Property(l => l.BoqId).IsRequired();
                links.Property(l => l.SectionId).IsRequired();
                links.Property(l => l.SubsectionId);

                // Backs no-double-count: a (boq, section, subsection) triple maps to at most one item.
                links.HasIndex(l => new { l.BoqId, l.SectionId, l.SubsectionId }).IsUnique();
            });

            items.Navigation(i => i.Links)
                .HasField("_links")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(c => c.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit fields stamped by the unit of work.
        builder.Property(c => c.CreatedOn).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.ModifiedOn).IsRequired();
        builder.Property(c => c.ModifiedBy).IsRequired();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(c => c.DomainEvents);
    }
}
