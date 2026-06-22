using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillOfQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bills_of_quantities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BidId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PricingCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate_base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    exchange_rate_quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    exchange_rate_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    exchange_rate_as_of = table.Column<DateOnly>(type: "date", nullable: true),
                    SubmittedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bills_of_quantities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "boq_sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BoqId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boq_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boq_sections_bills_of_quantities_BoqId",
                        column: x => x.BoqId,
                        principalTable: "bills_of_quantities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "boq_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boq_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boq_line_items_boq_sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "boq_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bills_of_quantities_BidId_Version",
                table: "bills_of_quantities",
                columns: new[] { "BidId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_boq_line_items_SectionId",
                table: "boq_line_items",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_boq_sections_BoqId",
                table: "boq_sections",
                column: "BoqId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boq_line_items");

            migrationBuilder.DropTable(
                name: "boq_sections");

            migrationBuilder.DropTable(
                name: "bills_of_quantities");
        }
    }
}
