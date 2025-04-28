using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArzanGo.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentId3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentSettingId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentSettingsPaymentSettingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentSettingsPaymentSettingId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentSettingId",
                table: "Orders",
                column: "PaymentSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingId",
                table: "Orders",
                column: "PaymentSettingId",
                principalTable: "PaymentSettings",
                principalColumn: "PaymentSettingId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentSettingId",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentSettingsPaymentSettingId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentSettingsPaymentSettingId",
                table: "Orders",
                column: "PaymentSettingsPaymentSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentSettings_PaymentSettingsPaymentSettingId",
                table: "Orders",
                column: "PaymentSettingsPaymentSettingId",
                principalTable: "PaymentSettings",
                principalColumn: "PaymentSettingId");
        }
    }
}
