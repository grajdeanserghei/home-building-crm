using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "contractors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "contractors");
        }
    }
}
