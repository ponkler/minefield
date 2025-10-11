using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minefield.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoinFlipCooldownAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessagesSinceCoinFlip",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessagesSinceCoinFlip",
                table: "Users");
        }
    }
}
