using HomeProjectManagement.Domain.Bids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeProjectManagement.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="Bid"/> aggregate root and its owned discussion log.</summary>
public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(b => b.Id);
        // Ids are generated in the domain (BidId.New()), never by the database.
        builder.Property(b => b.Id).ValueGeneratedNever();

        // References to other aggregates are held by id (Guid columns via the strongly-typed
        // id convention), not as EF navigations — aggregates never load one another.
        builder.Property(b => b.WorkPackageId).IsRequired();
        builder.Property(b => b.ContractorId).IsRequired();

        // A contractor may submit several bids on one work package (variants such as "Premium" /
        // "Buget"). Non-unique indexes back the per-work-package and per-contractor list queries.
        // The WorkPackageId index is named explicitly so EF keeps it distinct from the filtered
        // "one Selected per work package" index below (which is also keyed on WorkPackageId).
        builder.HasIndex(b => b.WorkPackageId, "IX_bids_WorkPackageId");
        builder.HasIndex(b => b.ContractorId);

        // At most one Selected bid per work package. A filtered unique index enforces it at the
        // database level, backing the application-service coordination that rejects the rivals.
        builder.HasIndex(b => b.WorkPackageId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Selected'")
            .HasDatabaseName("IX_bids_WorkPackageId_Selected");

        // Persist the enum as its string name.
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(b => b.FirstContactedOn);
        builder.Property(b => b.ExpectedBoqDate);
        builder.Property(b => b.Summary).HasMaxLength(1000);
        builder.Property(b => b.Label).HasMaxLength(120);

        // Discussion notes are internal entities owned by the bid: a child table whose rows live
        // and die with the bid and are loaded with it.
        builder.OwnsMany(b => b.Notes, notes =>
        {
            notes.ToTable("bid_discussion_notes");

            notes.WithOwner().HasForeignKey("BidId");
            notes.HasKey(n => n.Id);
            notes.Property(n => n.Id).ValueGeneratedNever();

            notes.Property(n => n.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            notes.Property(n => n.OccurredOn).IsRequired();
            notes.Property(n => n.AuthorId).IsRequired();
            notes.Property(n => n.Content).HasMaxLength(4000).IsRequired();

            notes.HasIndex("BidId");
        });

        // The notes collection is mutated only through the aggregate; EF reaches the backing field.
        builder.Navigation(b => b.Notes)
            .HasField("_notes")
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
