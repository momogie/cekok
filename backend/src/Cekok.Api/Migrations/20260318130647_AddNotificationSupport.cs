using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cekok.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyEmail",
                table: "Applications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotifyEmailAddress",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyTelegram",
                table: "Applications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotifyTelegramChatId",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyEmail",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "NotifyEmailAddress",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "NotifyTelegram",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "NotifyTelegramChatId",
                table: "Applications");
        }
    }
}
