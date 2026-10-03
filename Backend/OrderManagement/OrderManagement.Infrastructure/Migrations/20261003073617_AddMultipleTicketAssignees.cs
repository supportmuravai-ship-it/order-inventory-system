using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleTicketAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTickets_AspNetUsers_AssignedToUserId",
                table: "OrderTickets");

            migrationBuilder.DropIndex(
                name: "IX_OrderTickets_AssignedToUserId_Status",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "OrderTickets");

            migrationBuilder.CreateTable(
                name: "OrderTicketAssignees",
                columns: table => new
                {
                    OrderTicketId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTicketAssignees", x => new { x.OrderTicketId, x.UserId });
                    table.ForeignKey(
                        name: "FK_OrderTicketAssignees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTicketAssignees_OrderTickets_OrderTicketId",
                        column: x => x.OrderTicketId,
                        principalTable: "OrderTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTickets_StoreId_Status",
                table: "OrderTickets",
                columns: new[] { "StoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTicketAssignees_UserId",
                table: "OrderTicketAssignees",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTicketAssignees");

            migrationBuilder.DropIndex(
                name: "IX_OrderTickets_StoreId_Status",
                table: "OrderTickets");

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "OrderTickets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTickets_AssignedToUserId_Status",
                table: "OrderTickets",
                columns: new[] { "AssignedToUserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTickets_AspNetUsers_AssignedToUserId",
                table: "OrderTickets",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
