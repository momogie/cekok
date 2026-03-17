using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cekok.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHostnameToServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hostname",
                table: "Servers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hostname",
                table: "Servers");
        }
    }
}
