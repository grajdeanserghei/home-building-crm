using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleBidsPerContractorAddLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bids_WorkPackageId_ContractorId",
                table: "bids");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "bids",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bids_ContractorId",
                table: "bids",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_bids_WorkPackageId",
                table: "bids",
                column: "WorkPackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bids_ContractorId",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "IX_bids_WorkPackageId",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "bids");

            migrationBuilder.CreateIndex(
                name: "IX_bids_WorkPackageId_ContractorId",
                table: "bids",
                columns: new[] { "WorkPackageId", "ContractorId" },
                unique: true);
        }
    }
}
