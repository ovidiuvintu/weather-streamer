using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStreamer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Simulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 70, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotStarted")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.Id);
                    table.CheckConstraint("CK_Simulations_Status", "Status IN ('NotStarted', 'InProgress', 'Completed')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_FileName_Status",
                table: "Simulations",
                columns: new[] { "FileName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_StartTime",
                table: "Simulations",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_Status",
                table: "Simulations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Simulations");
        }
    }
}
