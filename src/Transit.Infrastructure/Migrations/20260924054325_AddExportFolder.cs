using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExportFolder",
                table: "AppSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportFolder",
                table: "AppSettings");
        }
    }
}
