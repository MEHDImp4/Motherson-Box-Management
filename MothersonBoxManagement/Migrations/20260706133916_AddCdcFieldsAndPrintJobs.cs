using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCdcFieldsAndPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "BoxPrintJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoxId = table.Column<int>(type: "int", nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReprintReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxPrintJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoxPrintJobs_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoxPrintJobs_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_QrCodeValue",
                table: "Boxes",
                column: "QrCodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoxPrintJobs_BoxId",
                table: "BoxPrintJobs",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_BoxPrintJobs_RequestedByUserId",
                table: "BoxPrintJobs",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoxPrintJobs");

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
    }
}
