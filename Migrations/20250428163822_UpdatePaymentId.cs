using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArzanGo.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentMethodId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "PaymentSettings",
                newName: "PaymentSettingId");

            migrationBuilder.RenameColumn(
                name: "PaymentSettingsPaymentMethodId",
                table: "Orders",
                newName: "PaymentSettingsPaymentSettingId");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "Orders",
                newName: "PaymentSettingId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PaymentSettingsPaymentMethodId",
                table: "Orders",
                newName: "IX_Orders_PaymentSettingsPaymentSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentSettingId",
                table: "Orders",
                column: "PaymentSettingsPaymentSettingId",
                principalTable: "PaymentSettings",
                principalColumn: "PaymentSettingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentSettingId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "PaymentSettingId",
                table: "PaymentSettings",
                newName: "PaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "PaymentSettingsPaymentSettingId",
                table: "Orders",
                newName: "PaymentSettingsPaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "PaymentSettingId",
                table: "Orders",
                newName: "PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PaymentSettingsPaymentSettingId",
                table: "Orders",
                newName: "IX_Orders_PaymentSettingsPaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentMethodId",
                table: "Orders",
                column: "PaymentSettingsPaymentMethodId",
                principalTable: "PaymentSettings",
                principalColumn: "PaymentMethodId");
        }
    }
}
