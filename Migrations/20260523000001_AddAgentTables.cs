using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId    = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Endpoint     = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_RequestId_Endpoint",
                table: "IdempotencyRecords",
                columns: new[] { "RequestId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "AgentActionAudits",
                columns: table => new
                {
                    Id         = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId  = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Endpoint   = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Source     = table.Column<string>(type: "nvarchar(64)",  maxLength: 64,  nullable: false),
                    TargetId   = table.Column<int>(type: "int", nullable: false),
                    ActionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentActionAudits", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AgentActionAudits");
            migrationBuilder.DropIndex(name: "IX_IdempotencyRecords_RequestId_Endpoint", table: "IdempotencyRecords");
            migrationBuilder.DropTable(name: "IdempotencyRecords");
        }
    }
}
