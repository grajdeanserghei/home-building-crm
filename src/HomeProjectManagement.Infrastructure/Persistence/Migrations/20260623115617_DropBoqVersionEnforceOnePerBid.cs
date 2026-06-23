using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropBoqVersionEnforceOnePerBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Moving from many BoQ versions per bid to at most one. Collapse any existing duplicates
            // first, or the unique index below cannot be created: keep the Accepted BoQ per bid (the
            // one a Contract may reference) and otherwise the highest version; drop the rest. Their
            // sections/subsections/line items cascade away via their owned-entity foreign keys.
            migrationBuilder.Sql("""
                DELETE FROM bills_of_quantities
                WHERE "Id" IN (
                    SELECT "Id" FROM (
                        SELECT "Id",
                               ROW_NUMBER() OVER (
                                   PARTITION BY "BidId"
                                   ORDER BY (CASE WHEN "Status" = 'Accepted' THEN 0 ELSE 1 END), "Version" DESC
                               ) AS rn
                        FROM bills_of_quantities
                    ) ranked
                    WHERE ranked.rn > 1
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_bills_of_quantities_BidId_Version",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "bills_of_quantities");

            migrationBuilder.CreateIndex(
                name: "IX_bills_of_quantities_BidId",
                table: "bills_of_quantities",
                column: "BidId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bills_of_quantities_BidId",
                table: "bills_of_quantities");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "bills_of_quantities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_bills_of_quantities_BidId_Version",
                table: "bills_of_quantities",
                columns: new[] { "BidId", "Version" },
                unique: true);
        }
    }
}
