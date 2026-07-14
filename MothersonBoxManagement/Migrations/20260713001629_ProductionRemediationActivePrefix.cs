using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class ProductionRemediationActivePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [BoxTemplates] WHERE LEN([PackagePrefixPattern]) > 450)
                    THROW 51000, 'PackagePrefixPattern exceeds 450 characters. Clean the data before migrating.', 1;

                UPDATE [BoxTemplates]
                SET [PackagePrefixPattern] = UPPER(LTRIM(RTRIM([PackagePrefixPattern])))
                WHERE [PackagePrefixPattern] IS NOT NULL;

                IF EXISTS (
                    SELECT [PackagePrefixPattern]
                    FROM [BoxTemplates]
                    WHERE [IsActive] = 1 AND [PackagePrefixPattern] IS NOT NULL
                    GROUP BY [PackagePrefixPattern]
                    HAVING COUNT(*) > 1)
                    THROW 51001, 'Duplicate active package prefixes exist. Resolve them before migrating.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PackagePrefixPattern",
                table: "BoxTemplates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoxTemplates_PackagePrefixPattern",
                table: "BoxTemplates",
                column: "PackagePrefixPattern",
                unique: true,
                filter: "[IsActive] = 1 AND [PackagePrefixPattern] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoxTemplates_PackagePrefixPattern",
                table: "BoxTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "PackagePrefixPattern",
                table: "BoxTemplates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
