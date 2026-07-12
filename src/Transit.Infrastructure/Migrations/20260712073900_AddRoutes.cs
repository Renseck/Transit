using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FromLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    OutboundKm = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReturnKm = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_WorkLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_WorkLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromLocationId",
                table: "Routes",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToLocationId",
                table: "Routes",
                column: "ToLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Routes");
        }
    }
}
