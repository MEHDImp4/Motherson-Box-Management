using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarcodeConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoxPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BoxDatePattern = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BoxRandomLength = table.Column<int>(type: "int", nullable: false),
                    PackagePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PackageMinLength = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeConfigurations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BarcodeConfigurations",
                columns: new[] { "Id", "BoxDatePattern", "BoxPrefix", "BoxRandomLength", "CreatedAt", "PackageMinLength", "PackagePrefix", "UpdatedAt" },
                values: new object[] { 1, "yyyyMMdd", "BOX-", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeConfigurations");
        }
    }
}
