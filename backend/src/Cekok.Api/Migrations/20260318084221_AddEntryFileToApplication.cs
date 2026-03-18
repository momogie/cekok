using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cekok.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryFileToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntryFile",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryFile",
                table: "Applications");
        }
    }
}
