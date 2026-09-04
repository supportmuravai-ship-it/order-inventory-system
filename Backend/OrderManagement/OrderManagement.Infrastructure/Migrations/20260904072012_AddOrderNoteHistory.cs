using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNoteHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderNoteHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    NoteType = table.Column<int>(type: "int", nullable: false),
                    OldText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNoteHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderNoteHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderNoteHistories_OrderId",
                table: "OrderNoteHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNoteHistories_OrderId_NoteType_ChangedAtUtc",
                table: "OrderNoteHistories",
                columns: new[] { "OrderId", "NoteType", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderNoteHistories");
        }
    }
}
