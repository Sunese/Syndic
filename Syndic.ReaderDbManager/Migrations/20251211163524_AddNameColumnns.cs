using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndic.ReaderDbManager.Migrations
{
    /// <inheritdoc />
    public partial class AddNameColumnns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChannelTitle",
                table: "subscriptions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelTitle",
                table: "subscriptions");
        }
    }
}
