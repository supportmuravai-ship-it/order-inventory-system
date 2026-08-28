using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackingHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OldTrackingNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewTrackingNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingHistories_OrderId",
                table: "TrackingHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingHistories_OrderId_ChangedAtUtc",
                table: "TrackingHistories",
                columns: new[] { "OrderId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackingHistories");
        }
    }
}
