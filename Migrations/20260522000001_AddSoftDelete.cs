using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WicStockDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WicStockDevices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReturnDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ReturnDevices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LoanerDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LoanerDevices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDeleted",  table: "WicStockDevices");
            migrationBuilder.DropColumn(name: "DeletedAt",  table: "WicStockDevices");
            migrationBuilder.DropColumn(name: "IsDeleted",  table: "ReturnDevices");
            migrationBuilder.DropColumn(name: "DeletedAt",  table: "ReturnDevices");
            migrationBuilder.DropColumn(name: "IsDeleted",  table: "LoanerDevices");
            migrationBuilder.DropColumn(name: "DeletedAt",  table: "LoanerDevices");
        }
    }
}
