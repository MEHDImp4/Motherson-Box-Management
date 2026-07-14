using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterConfigurationAndExtendPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "BoxPrintJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "BoxPrintJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadType",
                table: "BoxPrintJobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintedAt",
                table: "BoxPrintJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterUncPath",
                table: "BoxPrintJobs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "BoxPrintJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "BoxPrintJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkstationId",
                table: "BoxPrintJobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrinterConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PcName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrinterUncPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoxPrintJobs_WorkstationId",
                table: "BoxPrintJobs",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterConfigurations_Code",
                table: "PrinterConfigurations",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BoxPrintJobs_PrinterConfigurations_WorkstationId",
                table: "BoxPrintJobs",
                column: "WorkstationId",
                principalTable: "PrinterConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoxPrintJobs_PrinterConfigurations_WorkstationId",
                table: "BoxPrintJobs");

            migrationBuilder.DropTable(
                name: "PrinterConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_BoxPrintJobs_WorkstationId",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "PayloadType",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "PrintedAt",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "PrinterUncPath",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "BoxPrintJobs");

            migrationBuilder.DropColumn(
                name: "WorkstationId",
                table: "BoxPrintJobs");
        }
    }
}
