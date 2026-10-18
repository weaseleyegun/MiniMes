using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentCode = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SpindleRpm = table.Column<double>(type: "REAL", nullable: false),
                    FeedRate = table.Column<double>(type: "REAL", nullable: false),
                    SpindleLoadPct = table.Column<double>(type: "REAL", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLogs_EquipmentCode_CollectedAt",
                table: "EquipmentLogs",
                columns: new[] { "EquipmentCode", "CollectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentLogs");
        }
    }
}
