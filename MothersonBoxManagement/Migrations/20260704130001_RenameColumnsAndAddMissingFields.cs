using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumnsAndAddMissingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boxes_Users_ClosedByUserId",
                table: "Boxes");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Boxes",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "ClosedByUserId",
                table: "Boxes",
                newName: "CompletedByUserId");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                table: "Boxes",
                newName: "CompletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Boxes_ClosedByUserId",
                table: "Boxes",
                newName: "IX_Boxes_CompletedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkstationName",
                table: "BoxPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "Boxes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedAt",
                table: "Boxes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlockedByUserId",
                table: "Boxes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionMode",
                table: "Boxes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "BoxAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageBarcode",
                table: "BoxAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValue",
                table: "BoxAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "BoxAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedBoxId",
                table: "BoxAuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_BlockedByUserId",
                table: "Boxes",
                column: "BlockedByUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Boxes_Users_BlockedByUserId",
                table: "Boxes",
                column: "BlockedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boxes_Users_CompletedByUserId",
                table: "Boxes",
                column: "CompletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoxAuditLogs_Boxes_RelatedBoxId",
                table: "BoxAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Boxes_Users_BlockedByUserId",
                table: "Boxes");

            migrationBuilder.DropForeignKey(
                name: "FK_Boxes_Users_CompletedByUserId",
                table: "Boxes");

            migrationBuilder.DropIndex(
                name: "IX_Boxes_BlockedByUserId",
                table: "Boxes");

            migrationBuilder.DropIndex(
                name: "IX_BoxAuditLogs_RelatedBoxId",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkstationName",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "BlockedAt",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "BlockedByUserId",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "CompletionMode",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "PackageBarcode",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "PreviousValue",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "BoxAuditLogs");

            migrationBuilder.DropColumn(
                name: "RelatedBoxId",
                table: "BoxAuditLogs");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Boxes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CompletedByUserId",
                table: "Boxes",
                newName: "ClosedByUserId");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "Boxes",
                newName: "ClosedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Boxes_CompletedByUserId",
                table: "Boxes",
                newName: "IX_Boxes_ClosedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boxes_Users_ClosedByUserId",
                table: "Boxes",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
