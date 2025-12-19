using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gauniv.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimePlayedInMinutes",
                table: "UserGame",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayersConnectedSimultaneously",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SizeInMB",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimePlayedInMinutes",
                table: "UserGame");

            migrationBuilder.DropColumn(
                name: "MaxPlayersConnectedSimultaneously",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "SizeInMB",
                table: "Games");
        }
    }
}
