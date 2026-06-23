using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractor_trades",
                columns: table => new
                {
                    TradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_trades", x => new { x.ContractorId, x.TradeId });
                    table.ForeignKey(
                        name: "FK_contractor_trades_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "work_package_trades",
                columns: table => new
                {
                    TradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkPackageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_package_trades", x => new { x.WorkPackageId, x.TradeId });
                    table.ForeignKey(
                        name: "FK_work_package_trades_work_packages_WorkPackageId",
                        column: x => x.WorkPackageId,
                        principalTable: "work_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "trades",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "IsActive", "ModifiedBy", "ModifiedOn", "Name" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000001"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Zidărie" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000002"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Structură / Beton" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000003"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instalații Electrice" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000004"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instalații Sanitare" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000005"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instalații Termice" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000006"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instalații Răcire / Ventilare" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000007"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tâmplărie" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000008"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Interioare / Finisaje" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000009"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Acoperiș / Învelitori" },
                    { new Guid("a1b2c3d4-0000-0000-0000-000000000010"), null, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("00000000-0000-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Izolații" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_trades_TradeId",
                table: "contractor_trades",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_trades_Name",
                table: "trades",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_package_trades_TradeId",
                table: "work_package_trades",
                column: "TradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_trades");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "work_package_trades");
        }
    }
}
