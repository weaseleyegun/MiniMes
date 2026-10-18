using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAndCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Diameter = table.Column<double>(type: "REAL", nullable: false),
                    CutLength = table.Column<double>(type: "REAL", nullable: false),
                    FluteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StandardCycleTimeSec = table.Column<double>(type: "REAL", nullable: true),
                    StandardUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCycles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemCode = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentCode = table.Column<string>(type: "TEXT", nullable: false),
                    LotNo = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CycleTimeSec = table.Column<double>(type: "REAL", nullable: false),
                    IsGood = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpindleLoadAvg = table.Column<double>(type: "REAL", nullable: false),
                    SpindleLoadMax = table.Column<double>(type: "REAL", nullable: false),
                    SpindleRpmAvg = table.Column<double>(type: "REAL", nullable: false),
                    FeedRateAvg = table.Column<double>(type: "REAL", nullable: false),
                    VibrationMax = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCycles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                table: "Items",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCycles_IsGood",
                table: "ProductionCycles",
                column: "IsGood");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCycles_ItemCode_StartedAt",
                table: "ProductionCycles",
                columns: new[] { "ItemCode", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ProductionCycles");
        }
    }
}
