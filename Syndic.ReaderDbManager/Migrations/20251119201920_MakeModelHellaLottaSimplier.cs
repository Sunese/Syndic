using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndic.ReaderDbManager.Migrations
{
    /// <inheritdoc />
    public partial class MakeModelHellaLottaSimplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_feeds_FeedId",
                table: "subscriptions");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "feeds");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_CategoryId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_FeedId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_UserId_FeedId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "FeedId",
                table: "subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "ChannelUrl",
                table: "subscriptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_ChannelUrl",
                table: "subscriptions",
                columns: new[] { "UserId", "ChannelUrl" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_UserId_ChannelUrl",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ChannelUrl",
                table: "subscriptions");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeedId",
                table: "subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FeedUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastFetched = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Published = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Guid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articles_feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_CategoryId",
                table: "subscriptions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_FeedId",
                table: "subscriptions",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_FeedId",
                table: "subscriptions",
                columns: new[] { "UserId", "FeedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articles_FeedId",
                table: "articles",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_FeedId_Guid",
                table: "articles",
                columns: new[] { "FeedId", "Guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articles_Guid",
                table: "articles",
                column: "Guid");

            migrationBuilder.CreateIndex(
                name: "IX_articles_PublishedAt",
                table: "articles",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_feeds_LastFetched",
                table: "feeds",
                column: "LastFetched");

            migrationBuilder.CreateIndex(
                name: "IX_feeds_Status",
                table: "feeds",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_feeds_FeedId",
                table: "subscriptions",
                column: "FeedId",
                principalTable: "feeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
