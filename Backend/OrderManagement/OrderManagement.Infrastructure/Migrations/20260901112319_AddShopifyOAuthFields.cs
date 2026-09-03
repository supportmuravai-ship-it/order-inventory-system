using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopifyOAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShopifyAccessTokenEncrypted",
                table: "Stores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShopifyAccessTokenExpiresAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShopifyConnectedAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopifyGrantedScopes",
                table: "Stores",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopifyRefreshTokenEncrypted",
                table: "Stores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShopifyRefreshTokenExpiresAtUtc",
                table: "Stores",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopifyAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ShopifyAccessTokenExpiresAtUtc",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ShopifyConnectedAtUtc",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ShopifyGrantedScopes",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ShopifyRefreshTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ShopifyRefreshTokenExpiresAtUtc",
                table: "Stores");
        }
    }
}
