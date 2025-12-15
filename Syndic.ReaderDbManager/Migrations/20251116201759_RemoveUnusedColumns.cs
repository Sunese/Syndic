using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndic.ReaderDbManager.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_IsActive",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_articles_IsFavorite",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "IX_articles_IsRead",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "UnreadCount",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "feeds");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "articles");

            migrationBuilder.RenameColumn(
                name: "LastPublished",
                table: "feeds",
                newName: "Published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Published",
                table: "feeds",
                newName: "LastPublished");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UnreadCount",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "feeds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_IsActive",
                table: "subscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_articles_IsFavorite",
                table: "articles",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_articles_IsRead",
                table: "articles",
                column: "IsRead");
        }
    }
}
