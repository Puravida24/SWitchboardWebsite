using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T4_ClickEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClickEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VisitorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    ViewportW = table.Column<int>(type: "integer", nullable: false),
                    ViewportH = table.Column<int>(type: "integer", nullable: false),
                    PageW = table.Column<int>(type: "integer", nullable: false),
                    PageH = table.Column<int>(type: "integer", nullable: false),
                    Selector = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TagName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ElementText = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ElementHref = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MouseButton = table.Column<int>(type: "integer", nullable: false),
                    IsRage = table.Column<bool>(type: "boolean", nullable: false),
                    IsDead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickEvents", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 39, 55, 584, DateTimeKind.Utc).AddTicks(2140));

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_IsDead",
                table: "ClickEvents",
                column: "IsDead",
                filter: "\"IsDead\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_IsRage",
                table: "ClickEvents",
                column: "IsRage",
                filter: "\"IsRage\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_Path_Ts",
                table: "ClickEvents",
                columns: new[] { "Path", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_SessionId",
                table: "ClickEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_SessionId_Selector_Ts",
                table: "ClickEvents",
                columns: new[] { "SessionId", "Selector", "Ts" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickEvents");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 19, 3, 982, DateTimeKind.Utc).AddTicks(3530));
        }
    }
}
