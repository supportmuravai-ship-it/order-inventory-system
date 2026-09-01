using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopifySyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReconciliationAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastShopifyError",
                table: "Stores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessfulSyncAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWebhookReceivedAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReconciliationAtUtc",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "LastShopifyError",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulSyncAtUtc",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "LastWebhookReceivedAtUtc",
                table: "Stores");
        }
    }
}
