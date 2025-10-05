using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minefield.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedImmuneProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImmune",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsImmune",
                table: "Users");
        }
    }
}
