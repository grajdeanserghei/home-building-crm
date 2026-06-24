using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetScopeAndApartmentUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApartmentUnits",
                table: "projects",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Existing bills of quantities predate budget scoping; default them to the whole building
            // so their persisted scope is a valid BudgetScopeKind (an empty string would not parse).
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "bills_of_quantities",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "EntireBuilding");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApartmentUnits",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "bills_of_quantities");
        }
    }
}
