using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLineItemVatRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing line items are backfilled with the standard 21% VAT rate (the default
            // applied to every line) rather than 0.
            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate_percentage",
                table: "boq_line_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 21m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vat_rate_percentage",
                table: "boq_line_items");
        }
    }
}
