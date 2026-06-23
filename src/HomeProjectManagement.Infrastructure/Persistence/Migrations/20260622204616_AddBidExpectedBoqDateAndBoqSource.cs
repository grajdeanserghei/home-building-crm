using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeProjectManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBidExpectedBoqDateAndBoqSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceContentHash",
                table: "bills_of_quantities",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_file_name",
                table: "bills_of_quantities",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_document_uploaded_by",
                table: "bills_of_quantities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "source_document_uploaded_on",
                table: "bills_of_quantities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_url",
                table: "bills_of_quantities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedBoqDate",
                table: "bids",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bills_of_quantities_SourceContentHash",
                table: "bills_of_quantities",
                column: "SourceContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bills_of_quantities_SourceContentHash",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "SourceContentHash",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "source_document_file_name",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "source_document_uploaded_by",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "source_document_uploaded_on",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "source_document_url",
                table: "bills_of_quantities");

            migrationBuilder.DropColumn(
                name: "ExpectedBoqDate",
                table: "bids");
        }
    }
}
