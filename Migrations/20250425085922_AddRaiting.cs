using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArzanGo.Migrations
{
    /// <inheritdoc />
    public partial class AddRaiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Raiting",
                table: "Users",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Raiting",
                table: "Users");
        }
    }
}
