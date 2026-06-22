using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPackageScopeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_package_scope_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Requirement = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    WorkPackageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_package_scope_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_package_scope_items_work_packages_WorkPackageId",
                        column: x => x.WorkPackageId,
                        principalTable: "work_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_package_scope_items_WorkPackageId_Name",
                table: "work_package_scope_items",
                columns: new[] { "WorkPackageId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_package_scope_items");
        }
    }
}
