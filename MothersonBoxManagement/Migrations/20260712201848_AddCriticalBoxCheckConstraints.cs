using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MothersonBoxManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalBoxCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Boxes]
                    WHERE [ExpectedQuantity] <= 0
                       OR [CurrentQuantity] < 0
                       OR [CurrentQuantity] > [ExpectedQuantity]
                       OR [Height] <= 0
                       OR [Width] <= 0
                       OR [Depth] <= 0
                )
                    THROW 51001, 'Cannot add critical box constraints: existing Boxes contain invalid quantities or dimensions.', 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Boxes_CurrentQuantity_Range",
                table: "Boxes",
                sql: "[CurrentQuantity] >= 0 AND [CurrentQuantity] <= [ExpectedQuantity]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Boxes_Dimensions_Positive",
                table: "Boxes",
                sql: "[Height] > 0 AND [Width] > 0 AND [Depth] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Boxes_ExpectedQuantity_Positive",
                table: "Boxes",
                sql: "[ExpectedQuantity] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Boxes_CurrentQuantity_Range",
                table: "Boxes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Boxes_Dimensions_Positive",
                table: "Boxes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Boxes_ExpectedQuantity_Positive",
                table: "Boxes");
        }
    }
}
