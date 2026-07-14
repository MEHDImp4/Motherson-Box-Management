using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AllowHistoricalPackageReassociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoxPackages_PackageBarcode",
                table: "BoxPackages");

            migrationBuilder.CreateIndex(
                name: "IX_BoxPackages_PackageBarcode",
                table: "BoxPackages",
                column: "PackageBarcode",
                unique: true,
                filter: "[IsRemoved] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT [PackageBarcode]
                    FROM [BoxPackages]
                    GROUP BY [PackageBarcode]
                    HAVING COUNT(*) > 1
                )
                    THROW 51000, 'Cannot restore global package-barcode uniqueness while historical reassociations exist.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_BoxPackages_PackageBarcode",
                table: "BoxPackages");

            migrationBuilder.CreateIndex(
                name: "IX_BoxPackages_PackageBarcode",
                table: "BoxPackages",
                column: "PackageBarcode",
                unique: true);
        }
    }
}
