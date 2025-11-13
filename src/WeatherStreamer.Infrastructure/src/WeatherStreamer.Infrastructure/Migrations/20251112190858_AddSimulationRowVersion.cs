using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStreamer.Infrastructure.src.WeatherStreamer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Simulations",
                type: "BLOB",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Simulations");
        }
    }
}
