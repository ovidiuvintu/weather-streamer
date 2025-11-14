using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStreamer.Infrastructure.Migrations
{
    public partial class AddIsDeletedAndAuditAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add IsDeleted column to Simulations
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Simulations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Add Action column to AuditEntries
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditEntries",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

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
