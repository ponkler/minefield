using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minefield.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedCofferTableForPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coffers",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coffers", x => x.ServerId);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => new { x.ServerId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Tickets_Coffers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Coffers",
                        principalColumn: "ServerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tickets_Users_UserId_ServerId",
                        columns: x => new { x.UserId, x.ServerId },
                        principalTable: "Users",
                        principalColumns: new[] { "UserId", "ServerId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UserId_ServerId",
                table: "Tickets",
                columns: new[] { "UserId", "ServerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Coffers");
        }
    }
}
