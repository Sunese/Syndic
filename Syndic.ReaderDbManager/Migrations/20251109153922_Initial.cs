using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndic.ReaderDbManager.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FeedUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastFetched = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastPublished = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OIDCSubject = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Guid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscribedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_articles_IsFavorite",
                table: "articles",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_articles_IsRead",
                table: "articles",
                column: "IsRead");

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

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_CategoryId",
                table: "subscriptions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_FeedId",
                table: "subscriptions",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_IsActive",
                table: "subscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                table: "subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_FeedId",
                table: "subscriptions",
                columns: new[] { "UserId", "FeedId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "feeds");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
