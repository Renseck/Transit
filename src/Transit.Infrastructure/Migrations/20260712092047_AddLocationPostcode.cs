using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationPostcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "WorkLocations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "WorkLocations");
        }
    }
}
