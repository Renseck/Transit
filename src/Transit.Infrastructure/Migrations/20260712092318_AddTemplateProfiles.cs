using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    SheetSelector = table.Column<string>(type: "TEXT", nullable: false),
                    DataStartRow = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CellMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CellRef = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Format = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellMapping_TemplateProfiles_TemplateProfileId",
                        column: x => x.TemplateProfileId,
                        principalTable: "TemplateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColumnMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Field = table.Column<string>(type: "TEXT", nullable: false),
                    Column = table.Column<string>(type: "TEXT", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColumnMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColumnMapping_TemplateProfiles_TemplateProfileId",
                        column: x => x.TemplateProfileId,
                        principalTable: "TemplateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellMapping_TemplateProfileId",
                table: "CellMapping",
                column: "TemplateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMapping_TemplateProfileId",
                table: "ColumnMapping",
                column: "TemplateProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CellMapping");

            migrationBuilder.DropTable(
                name: "ColumnMapping");

            migrationBuilder.DropTable(
                name: "TemplateProfiles");
        }
    }
}
