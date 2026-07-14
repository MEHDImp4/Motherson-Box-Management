using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class CleanupDeadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boxes_QrCodeValue",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "AssociationResult",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "BoxScanValue",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "LastPrintStatus",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "LastPrintedAt",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "QrCodeValue",
                table: "Boxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssociationResult",
                table: "BoxPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoxScanValue",
                table: "BoxPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BoxPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPrintStatus",
                table: "Boxes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintedAt",
                table: "Boxes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Boxes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeValue",
                table: "Boxes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_QrCodeValue",
                table: "Boxes",
                column: "QrCodeValue",
                unique: true);
        }
    }
}
