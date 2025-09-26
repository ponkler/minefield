using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minefield.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CurrentOdds = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<int>(type: "INTEGER", nullable: false),
                    LifetimeCurrency = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMessages = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAlive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AegisCharges = table.Column<int>(type: "INTEGER", nullable: false),
                    MessagesSinceAegis = table.Column<int>(type: "INTEGER", nullable: false),
                    LifelineCharges = table.Column<int>(type: "INTEGER", nullable: false),
                    SymbioteCharges = table.Column<int>(type: "INTEGER", nullable: false),
                    LuckCharges = table.Column<int>(type: "INTEGER", nullable: false),
                    HasGuardian = table.Column<bool>(type: "INTEGER", nullable: false),
                    MessagesSinceGuardian = table.Column<int>(type: "INTEGER", nullable: false),
                    LifelineTargetId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LifelineTargetServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LifelineProviderId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LifelineProviderServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SacrificeTargetId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SacrificeTargetServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SacrificeProviderId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SacrificeProviderServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SymbioteTargetId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SymbioteTargetServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SymbioteProviderId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SymbioteProviderServerId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => new { x.UserId, x.ServerId });
                    table.ForeignKey(
                        name: "FK_Users_Users_LifelineTargetId_LifelineTargetServerId",
                        columns: x => new { x.LifelineTargetId, x.LifelineTargetServerId },
                        principalTable: "Users",
                        principalColumns: new[] { "UserId", "ServerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_SacrificeTargetId_SacrificeTargetServerId",
                        columns: x => new { x.SacrificeTargetId, x.SacrificeTargetServerId },
                        principalTable: "Users",
                        principalColumns: new[] { "UserId", "ServerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_SymbioteTargetId_SymbioteTargetServerId",
                        columns: x => new { x.SymbioteTargetId, x.SymbioteTargetServerId },
                        principalTable: "Users",
                        principalColumns: new[] { "UserId", "ServerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LifelineTargetId_LifelineTargetServerId",
                table: "Users",
                columns: new[] { "LifelineTargetId", "LifelineTargetServerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SacrificeTargetId_SacrificeTargetServerId",
                table: "Users",
                columns: new[] { "SacrificeTargetId", "SacrificeTargetServerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SymbioteTargetId_SymbioteTargetServerId",
                table: "Users",
                columns: new[] { "SymbioteTargetId", "SymbioteTargetServerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
