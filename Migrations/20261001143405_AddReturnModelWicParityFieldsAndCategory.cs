using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnModelWicParityFieldsAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Acknowledgement",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "WicStockDevices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ChargerReturned",
                table: "WicStockDevices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageComment",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageStatus",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBatterySwollen",
                table: "WicStockDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomerInducedDamage",
                table: "WicStockDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KID",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseEndDatePlanned",
                table: "WicStockDevices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupStatus",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PowerCableReturned",
                table: "WicStockDevices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrder",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ReturnDevices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WicStockDevicePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WicStockDeviceId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSignature = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WicStockDevicePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WicStockDevicePhotos_WicStockDevices_WicStockDeviceId",
                        column: x => x.WicStockDeviceId,
                        principalTable: "WicStockDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WicStockDevicePhotos_WicStockDeviceId",
                table: "WicStockDevicePhotos",
                column: "WicStockDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WicStockDevicePhotos");

            migrationBuilder.DropColumn(
                name: "Acknowledgement",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "ChargerReturned",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "DamageComment",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "DamageStatus",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "IsBatterySwollen",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "IsCustomerInducedDamage",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "KID",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "LeaseEndDatePlanned",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "PickupStatus",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "PowerCableReturned",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "WorkOrder",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ReturnDevices");
        }
    }
}
