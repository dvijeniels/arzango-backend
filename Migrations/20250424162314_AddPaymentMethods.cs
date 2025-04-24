using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArzanGo.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyingType",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentMethodId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentSettingsPaymentMethodId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentSettings",
                columns: table => new
                {
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSettings", x => x.PaymentMethodId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentSettingsPaymentMethodId",
                table: "Orders",
                column: "PaymentSettingsPaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentMethodId",
                table: "Orders",
                column: "PaymentSettingsPaymentMethodId",
                principalTable: "PaymentSettings",
                principalColumn: "PaymentMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentMethodId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "PaymentSettings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentSettingsPaymentMethodId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentSettingsPaymentMethodId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "BuyingType",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
