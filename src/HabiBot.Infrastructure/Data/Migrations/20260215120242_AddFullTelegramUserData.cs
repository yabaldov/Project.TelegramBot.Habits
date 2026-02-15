using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTelegramUserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramChatId",
                table: "Users");

            // Добавляем RegisteredAt с текущим временем для существующих записей
            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "TelegramFirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramLastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Добавляем TelegramUserId, копируя значения из TelegramChatId для существующих записей
            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Копируем данные из TelegramChatId в TelegramUserId для существующих пользователей
            migrationBuilder.Sql("UPDATE \"Users\" SET \"TelegramUserId\" = \"TelegramChatId\" WHERE \"TelegramUserId\" = 0");

            migrationBuilder.AddColumn<string>(
                name: "TelegramUserName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramFirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramLastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUserName",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramChatId",
                table: "Users",
                column: "TelegramChatId",
                unique: true);
        }
    }
}
