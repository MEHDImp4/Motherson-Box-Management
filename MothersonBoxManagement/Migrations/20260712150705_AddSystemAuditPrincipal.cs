using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAuditPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "FullName", "IsActive", "Matricule", "PasswordHash", "Role", "SecurityStamp", "UpdatedAt" },
                values: new object[] { -1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Application System", false, "SYSTEM", "LOGIN-DISABLED", "System", "SYSTEM-PRINCIPAL", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: -1);
        }
    }
}
