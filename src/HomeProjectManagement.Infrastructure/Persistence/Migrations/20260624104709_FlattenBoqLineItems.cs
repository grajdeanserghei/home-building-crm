using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FlattenBoqLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Line items move from two owned tables (a section's direct lines in boq_line_items, owned
            // by the section; a subsection's lines in boq_subsection_line_items) into a single flat
            // boq_line_items owned by the BoQ, each row carrying its SectionId + nullable SubsectionId.
            // The new owner key (BoqId) and SubsectionId are added nullable first so existing rows can be
            // back-filled, then BoqId is tightened to NOT NULL.
            migrationBuilder.AddColumn<Guid>(
                name: "BoqId",
                table: "boq_line_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubsectionId",
                table: "boq_line_items",
                type: "uuid",
                nullable: true);

            // Existing direct lines already carry SectionId — derive their BoqId from the owning section.
            migrationBuilder.Sql(@"
                UPDATE ""boq_line_items"" li
                SET ""BoqId"" = s.""BoqId""
                FROM ""boq_sections"" s
                WHERE li.""SectionId"" = s.""Id"";");

            // Migrate the subsection lines into the flat table, tagging each with its subsection's
            // SectionId and the owning BoQ. Their ids are preserved.
            migrationBuilder.Sql(@"
                INSERT INTO ""boq_line_items""
                    (""Id"", ""SectionId"", ""SubsectionId"", ""BoqId"", ""Description"", ""Quantity"",
                     ""UnitOfMeasureId"", ""Sequence"", ""Notes"", ""unit_price_amount"",
                     ""unit_price_currency"", ""vat_rate_percentage"")
                SELECT sli.""Id"", sub.""SectionId"", sli.""SubsectionId"", s.""BoqId"", sli.""Description"",
                       sli.""Quantity"", sli.""UnitOfMeasureId"", sli.""Sequence"", sli.""Notes"",
                       sli.""unit_price_amount"", sli.""unit_price_currency"", sli.""vat_rate_percentage""
                FROM ""boq_subsection_line_items"" sli
                JOIN ""boq_subsections"" sub ON sli.""SubsectionId"" = sub.""Id""
                JOIN ""boq_sections"" s ON sub.""SectionId"" = s.""Id"";");

            migrationBuilder.DropForeignKey(
                name: "FK_boq_line_items_boq_sections_SectionId",
                table: "boq_line_items");

            migrationBuilder.DropTable(
                name: "boq_subsection_line_items");

            // Defensive: drop any line that couldn't be tied to a BoQ (should be none after back-fill).
            migrationBuilder.Sql(@"DELETE FROM ""boq_line_items"" WHERE ""BoqId"" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "BoqId",
                table: "boq_line_items",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_boq_line_items_BoqId",
                table: "boq_line_items",
                column: "BoqId");

            migrationBuilder.CreateIndex(
                name: "IX_boq_line_items_SubsectionId",
                table: "boq_line_items",
                column: "SubsectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_boq_line_items_bills_of_quantities_BoqId",
                table: "boq_line_items",
                column: "BoqId",
                principalTable: "bills_of_quantities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_boq_line_items_bills_of_quantities_BoqId",
                table: "boq_line_items");

            migrationBuilder.DropIndex(
                name: "IX_boq_line_items_BoqId",
                table: "boq_line_items");

            migrationBuilder.DropIndex(
                name: "IX_boq_line_items_SubsectionId",
                table: "boq_line_items");

            migrationBuilder.DropColumn(
                name: "BoqId",
                table: "boq_line_items");

            migrationBuilder.DropColumn(
                name: "SubsectionId",
                table: "boq_line_items");

            migrationBuilder.CreateTable(
                name: "boq_subsection_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    SubsectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    vat_rate_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boq_subsection_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boq_subsection_line_items_boq_subsections_SubsectionId",
                        column: x => x.SubsectionId,
                        principalTable: "boq_subsections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boq_subsection_line_items_SubsectionId",
                table: "boq_subsection_line_items",
                column: "SubsectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_boq_line_items_boq_sections_SectionId",
                table: "boq_line_items",
                column: "SectionId",
                principalTable: "boq_sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
