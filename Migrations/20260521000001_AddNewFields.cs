using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WIC",
                table: "LoanerDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KID",
                table: "LoanerDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ChargerReturned",
                table: "ReturnDevices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PowerCableReturned",
                table: "ReturnDevices",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WIC", table: "LoanerDevices");
            migrationBuilder.DropColumn(name: "KID", table: "LoanerDevices");
            migrationBuilder.DropColumn(name: "ChargerReturned", table: "ReturnDevices");
            migrationBuilder.DropColumn(name: "PowerCableReturned", table: "ReturnDevices");
        }
    }
}
