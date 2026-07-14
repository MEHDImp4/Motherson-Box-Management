using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MothersonBoxManagement.Data;

#nullable disable

namespace MothersonBoxManagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260713120000_AddLocalPrintAgent")]
public partial class AddLocalPrintAgent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("PayloadVersion", "BoxPrintJobs", "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("LeaseTokenHash", "BoxPrintJobs", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>("LeaseExpiresAt", "BoxPrintJobs", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("NextAttemptAt", "BoxPrintJobs", "datetime2", nullable: true);
        migrationBuilder.AddColumn<byte[]>("RowVersion", "BoxPrintJobs", "rowversion", rowVersion: true, nullable: false);

        migrationBuilder.AddColumn<string>("PrintMode", "PrinterConfigurations", "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Windows");
        migrationBuilder.AddColumn<string>("MachineName", "PrinterConfigurations", "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("AgentTokenHash", "PrinterConfigurations", "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>("AgentVersion", "PrinterConfigurations", "nvarchar(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>("AvailablePrintersJson", "PrinterConfigurations", "nvarchar(max)", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<DateTime>("LastSeenAt", "PrinterConfigurations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("RevokedAt", "PrinterConfigurations", "datetime2", nullable: true);

        migrationBuilder.CreateTable(
            name: "PrintAgentPairingCodes",
            columns: table => new
            {
                Id = table.Column<int>("int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WorkstationId = table.Column<int>("int", nullable: false),
                CodeHash = table.Column<string>("nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>("datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>("datetime2", nullable: false),
                ConsumedAt = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PrintAgentPairingCodes", x => x.Id);
                table.ForeignKey("FK_PrintAgentPairingCodes_PrinterConfigurations_WorkstationId", x => x.WorkstationId, "PrinterConfigurations", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_PrinterConfigurations_AgentTokenHash", "PrinterConfigurations", "AgentTokenHash", unique: true, filter: "[AgentTokenHash] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_PrintAgentPairingCodes_CodeHash", "PrintAgentPairingCodes", "CodeHash", unique: true);
        migrationBuilder.CreateIndex("IX_PrintAgentPairingCodes_WorkstationId", "PrintAgentPairingCodes", "WorkstationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PrintAgentPairingCodes");
        migrationBuilder.DropIndex("IX_PrinterConfigurations_AgentTokenHash", "PrinterConfigurations");
        migrationBuilder.DropColumn("PayloadVersion", "BoxPrintJobs");
        migrationBuilder.DropColumn("LeaseTokenHash", "BoxPrintJobs");
        migrationBuilder.DropColumn("LeaseExpiresAt", "BoxPrintJobs");
        migrationBuilder.DropColumn("NextAttemptAt", "BoxPrintJobs");
        migrationBuilder.DropColumn("RowVersion", "BoxPrintJobs");
        migrationBuilder.DropColumn("PrintMode", "PrinterConfigurations");
        migrationBuilder.DropColumn("MachineName", "PrinterConfigurations");
        migrationBuilder.DropColumn("AgentTokenHash", "PrinterConfigurations");
        migrationBuilder.DropColumn("AgentVersion", "PrinterConfigurations");
        migrationBuilder.DropColumn("AvailablePrintersJson", "PrinterConfigurations");
        migrationBuilder.DropColumn("LastSeenAt", "PrinterConfigurations");
        migrationBuilder.DropColumn("RevokedAt", "PrinterConfigurations");
    }
}
