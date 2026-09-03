using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTicketOrderOptionalAndAddStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "OrderTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderTickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("""
        UPDATE t
        SET t.StoreId = o.StoreId
        FROM OrderTickets t
        INNER JOIN Orders o ON t.OrderId = o.Id
        WHERE t.StoreId IS NULL;
        """);

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "OrderTickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTickets_StoreId",
                table: "OrderTickets",
                column: "StoreId");

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

            migrationBuilder.DropIndex(
                name: "IX_OrderTickets_StoreId",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "OrderTickets");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderTickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
