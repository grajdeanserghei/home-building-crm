using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPackageIdToValuationItemLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable first so existing links can be backfilled before the NOT NULL constraint lands.
            migrationBuilder.AddColumn<Guid>(
                name: "WorkPackageId",
                table: "valuation_item_links",
                type: "uuid",
                nullable: true);

            // Backfill the work package each existing link's BoQ competes for: boq -> bid -> workPackage.
            migrationBuilder.Sql(
                """
                UPDATE valuation_item_links l
                SET "WorkPackageId" = b."WorkPackageId"
                FROM bills_of_quantities boq
                JOIN bids b ON b."Id" = boq."BidId"
                WHERE l."BoqId" = boq."Id";
                """);

            // Any link whose BoQ (or its bid) was deleted out from under it can't be resolved; drop it —
            // it no longer contributes to the read model and would violate the NOT NULL constraint.
            migrationBuilder.Sql(
                """DELETE FROM valuation_item_links WHERE "WorkPackageId" IS NULL;""");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkPackageId",
                table: "valuation_item_links",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkPackageId",
                table: "valuation_item_links");
        }
    }
}
