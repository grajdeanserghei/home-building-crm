using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLineItemIdToValuationItemLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_valuation_item_links_BoqId_SectionId_SubsectionId",
                table: "valuation_item_links");

            migrationBuilder.AddColumn<Guid>(
                name: "LineItemId",
                table: "valuation_item_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_valuation_item_links_BoqId_SectionId_SubsectionId_LineItemId",
                table: "valuation_item_links",
                columns: new[] { "BoqId", "SectionId", "SubsectionId", "LineItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_valuation_item_links_BoqId_SectionId_SubsectionId_LineItemId",
                table: "valuation_item_links");

            migrationBuilder.DropColumn(
                name: "LineItemId",
                table: "valuation_item_links");

            migrationBuilder.CreateIndex(
                name: "IX_valuation_item_links_BoqId_SectionId_SubsectionId",
                table: "valuation_item_links",
                columns: new[] { "BoqId", "SectionId", "SubsectionId" },
                unique: true);
        }
    }
}
