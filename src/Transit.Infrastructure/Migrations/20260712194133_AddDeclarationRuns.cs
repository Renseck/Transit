using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclarationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeclarationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodYear = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    XlsxFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    PdfFilePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeclarationRow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeclarationRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    WeekdayName = table.Column<string>(type: "TEXT", nullable: false),
                    WorkLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    CountsAsTravel = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnmarkedDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    RouteId = table.Column<int>(type: "INTEGER", nullable: true),
                    RouteName = table.Column<string>(type: "TEXT", nullable: true),
                    FromLocationName = table.Column<string>(type: "TEXT", nullable: true),
                    FromPostcode = table.Column<string>(type: "TEXT", nullable: true),
                    ToLocationName = table.Column<string>(type: "TEXT", nullable: true),
                    ToPostcode = table.Column<string>(type: "TEXT", nullable: true),
                    OutboundKm = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReturnKm = table.Column<decimal>(type: "TEXT", nullable: false),
                    Kilometers = table.Column<decimal>(type: "TEXT", nullable: false),
                    HasNoRoute = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationRow_DeclarationRuns_DeclarationRunId",
                        column: x => x.DeclarationRunId,
                        principalTable: "DeclarationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationRow_DeclarationRunId",
                table: "DeclarationRow",
                column: "DeclarationRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeclarationRow");

            migrationBuilder.DropTable(
                name: "DeclarationRuns");
        }
    }
}
