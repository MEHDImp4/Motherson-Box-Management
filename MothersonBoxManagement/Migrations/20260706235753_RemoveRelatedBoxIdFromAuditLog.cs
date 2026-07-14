using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRelatedBoxIdFromAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoxAuditLogs_Boxes_RelatedBoxId",
                table: "BoxAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_BoxAuditLogs_RelatedBoxId",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "RelatedBoxId",
                table: "BoxAuditLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatedBoxId",
                table: "BoxAuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoxAuditLogs_RelatedBoxId",
                table: "BoxAuditLogs",
                column: "RelatedBoxId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoxAuditLogs_Boxes_RelatedBoxId",
                table: "BoxAuditLogs",
                column: "RelatedBoxId",
                principalTable: "Boxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
