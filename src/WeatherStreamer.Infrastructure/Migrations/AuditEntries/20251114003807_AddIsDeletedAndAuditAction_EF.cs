using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStreamer.Infrastructure.Migrations.AuditEntries
{
    /// <inheritdoc />
    public partial class AddIsDeletedAndAuditAction_EF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Simulations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditEntries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AuditEntries");
        }
    }
}
