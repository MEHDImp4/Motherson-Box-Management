using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AuditRemediationSecurityAndTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Users SET SecurityStamp = REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '') WHERE SecurityStamp = ''");

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "BoxPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RemovalReason",
                table: "BoxPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAt",
                table: "BoxPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemovedByUserId",
                table: "BoxPackages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoxPackages_RemovedByUserId",
                table: "BoxPackages",
                column: "RemovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoxPackages_Users_RemovedByUserId",
                table: "BoxPackages",
                column: "RemovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoxPackages_Users_RemovedByUserId",
                table: "BoxPackages");

            migrationBuilder.DropIndex(
                name: "IX_BoxPackages_RemovedByUserId",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "RemovalReason",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "BoxPackages");

            migrationBuilder.DropColumn(
                name: "RemovedByUserId",
                table: "BoxPackages");
        }
    }
}
