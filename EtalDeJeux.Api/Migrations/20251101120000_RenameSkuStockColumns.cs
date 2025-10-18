using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtalDeJeux.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameSkuStockColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "stock_total",
                table: "skus",
                newName: "stockTotal");

            migrationBuilder.RenameColumn(
                name: "sold_qty",
                table: "skus",
                newName: "soldQty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "stockTotal",
                table: "skus",
                newName: "stock_total");

            migrationBuilder.RenameColumn(
                name: "soldQty",
                table: "skus",
                newName: "sold_qty");
        }
    }
}
