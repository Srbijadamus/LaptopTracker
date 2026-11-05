using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWicReturnTransferLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnDeviceId",
                table: "WicStockDevices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WicStockDevices_ReturnDeviceId",
                table: "WicStockDevices",
                column: "ReturnDeviceId",
                unique: true,
                filter: "[ReturnDeviceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_WicStockDevices_ReturnDevices_ReturnDeviceId",
                table: "WicStockDevices",
                column: "ReturnDeviceId",
                principalTable: "ReturnDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WicStockDevices_ReturnDevices_ReturnDeviceId",
                table: "WicStockDevices");

            migrationBuilder.DropIndex(
                name: "IX_WicStockDevices_ReturnDeviceId",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "ReturnDeviceId",
                table: "WicStockDevices");
        }
    }
}
