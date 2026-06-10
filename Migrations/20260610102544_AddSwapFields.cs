using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SwapDate",
                table: "WicStockDevices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwapSerialNumber",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwapStatus",
                table: "WicStockDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SwapDate",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "SwapSerialNumber",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "SwapStatus",
                table: "WicStockDevices");
        }
    }
}
