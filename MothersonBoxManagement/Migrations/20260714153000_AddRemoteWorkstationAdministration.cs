using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations;

public partial class AddRemoteWorkstationAdministration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DisplayName",
            table: "PrinterConfigurations",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastIpAddress",
            table: "PrinterConfigurations",
            type: "nvarchar(45)",
            maxLength: 45,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DisplayName", table: "PrinterConfigurations");
        migrationBuilder.DropColumn(name: "LastIpAddress", table: "PrinterConfigurations");
    }
}
