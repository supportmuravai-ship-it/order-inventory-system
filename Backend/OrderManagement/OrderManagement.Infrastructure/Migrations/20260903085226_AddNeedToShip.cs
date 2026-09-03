using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedToShip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTickets_Stores_StoreId",
                table: "OrderTickets");

            migrationBuilder.AddColumn<bool>(
                name: "NeedToShip",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTickets_Stores_StoreId",
                table: "OrderTickets",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTickets_Stores_StoreId",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "NeedToShip",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTickets_Stores_StoreId",
                table: "OrderTickets",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
