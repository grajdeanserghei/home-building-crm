using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionValuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "construction_valuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ValuationCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Appraiser = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    exchange_rate_base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate_quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    exchange_rate_as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    source_document_file_name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    source_document_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    source_document_uploaded_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_document_uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_construction_valuations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "valuation_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CatalogReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    vat_rate_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    BuiltArea = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GrossFloorArea = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UsableArea = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OwnRegieAdjustment = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valuation_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "construction_valuation_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ValuationCatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    estimated_without_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_without_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    estimated_with_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_with_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    completed_without_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    completed_without_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    completed_with_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    completed_with_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RemainingPercentage = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    remaining_without_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_without_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    remaining_with_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_with_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ConstructionValuationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_construction_valuation_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_construction_valuation_items_construction_valuations_Constr~",
                        column: x => x.ConstructionValuationId,
                        principalTable: "construction_valuations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_catalog_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PrintedNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CatalogSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CostWeight = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    unit_cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_cost_without_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_cost_without_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_cost_with_vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_cost_with_vat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValuationCatalogId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valuation_catalog_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_valuation_catalog_items_valuation_catalogs_ValuationCatalog~",
                        column: x => x.ValuationCatalogId,
                        principalTable: "valuation_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_item_links",
                columns: table => new
                {
                    ValuationCatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoqId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubsectionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valuation_item_links", x => new { x.ValuationCatalogItemId, x.Id });
                    table.ForeignKey(
                        name: "FK_valuation_item_links_valuation_catalog_items_ValuationCatal~",
                        column: x => x.ValuationCatalogItemId,
                        principalTable: "valuation_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_construction_valuation_items_ConstructionValuationId",
                table: "construction_valuation_items",
                column: "ConstructionValuationId");

            migrationBuilder.CreateIndex(
                name: "IX_construction_valuations_SourceContentHash",
                table: "construction_valuations",
                column: "SourceContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_construction_valuations_ValuationCatalogId",
                table: "construction_valuations",
                column: "ValuationCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_valuation_catalog_items_ValuationCatalogId",
                table: "valuation_catalog_items",
                column: "ValuationCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_valuation_catalogs_ProjectId",
                table: "valuation_catalogs",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_valuation_item_links_BoqId_SectionId_SubsectionId",
                table: "valuation_item_links",
                columns: new[] { "BoqId", "SectionId", "SubsectionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "construction_valuation_items");

            migrationBuilder.DropTable(
                name: "valuation_item_links");

            migrationBuilder.DropTable(
                name: "construction_valuations");

            migrationBuilder.DropTable(
                name: "valuation_catalog_items");

            migrationBuilder.DropTable(
                name: "valuation_catalogs");
        }
    }
}
