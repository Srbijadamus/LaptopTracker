using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnInspectionAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Acknowledgement",
                table: "ReturnDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageStatus",
                table: "ReturnDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBatterySwollen",
                table: "ReturnDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomerInducedDamage",
                table: "ReturnDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReturnDevicePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnDeviceId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnDevicePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnDevicePhotos_ReturnDevices_ReturnDeviceId",
                        column: x => x.ReturnDeviceId,
                        principalTable: "ReturnDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDevicePhotos_ReturnDeviceId",
                table: "ReturnDevicePhotos",
                column: "ReturnDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnDevicePhotos");

            migrationBuilder.DropColumn(
                name: "Acknowledgement",
                table: "ReturnDevices");

            migrationBuilder.DropColumn(
                name: "DamageStatus",
                table: "ReturnDevices");

            migrationBuilder.DropColumn(
                name: "IsBatterySwollen",
                table: "ReturnDevices");

            migrationBuilder.DropColumn(
                name: "IsCustomerInducedDamage",
                table: "ReturnDevices");
        }
    }
}
