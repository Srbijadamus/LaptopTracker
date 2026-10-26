using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWicLocationsModelsAndSiteColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "WicStockDevices",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "WicStockDevices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeClass = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WicLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Llid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CityLabel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeviceLocationKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WicLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationAliases_WicLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "WicLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationAliases_Alias",
                table: "LocationAliases",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationAliases_LocationId",
                table: "LocationAliases",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelName",
                table: "Models",
                column: "ModelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WicLocations_DeviceLocationKey",
                table: "WicLocations",
                column: "DeviceLocationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WicLocations_Llid",
                table: "WicLocations",
                column: "Llid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationAliases");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "WicLocations");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "WicStockDevices");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "WicStockDevices");
        }
    }
}
