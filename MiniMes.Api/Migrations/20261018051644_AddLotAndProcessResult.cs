using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLotAndProcessResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LotNo = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkOrderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lots_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessCode = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentCode = table.Column<string>(type: "TEXT", nullable: false),
                    GoodQty = table.Column<int>(type: "INTEGER", nullable: false),
                    DefectQty = table.Column<int>(type: "INTEGER", nullable: false),
                    CycleTimeSec = table.Column<double>(type: "REAL", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LotId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessResults_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_LotNo",
                table: "Lots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_WorkOrderId",
                table: "Lots",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessResults_LotId_ProcessCode",
                table: "ProcessResults",
                columns: new[] { "LotId", "ProcessCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessResults");

            migrationBuilder.DropTable(
                name: "Lots");
        }
    }
}
