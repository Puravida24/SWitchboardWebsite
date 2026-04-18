using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class Slice2_FormSubmissionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BouncedEmail",
                table: "FormSubmissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPhoenixAttemptAt",
                table: "FormSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhoenixSyncAttempts",
                table: "FormSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhoenixSyncStatus",
                table: "FormSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "FormSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 3, 40, 34, 895, DateTimeKind.Utc).AddTicks(5610));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BouncedEmail",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "LastPhoenixAttemptAt",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "PhoenixSyncAttempts",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "PhoenixSyncStatus",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "FormSubmissions");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 3, 1, 2, 313, DateTimeKind.Utc).AddTicks(1090));
        }
    }
}
