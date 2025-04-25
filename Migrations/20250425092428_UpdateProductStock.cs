using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArzanGo.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stok",
                table: "Products",
                newName: "Stock");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Products",
                newName: "Stok");
        }
    }
}
