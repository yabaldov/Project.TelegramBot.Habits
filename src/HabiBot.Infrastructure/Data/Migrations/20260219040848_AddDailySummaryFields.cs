using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySummaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "DailySummaryTime",
                table: "Users",
                type: "interval",
                nullable: true,
                defaultValue: new TimeSpan(21, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsDailySummaryEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailySummaryTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDailySummaryEnabled",
                table: "Users");
        }
    }
}
