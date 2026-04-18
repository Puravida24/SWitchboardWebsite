using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T1_VisitorAndSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VisitorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    EngagedTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    EventCount = table.Column<int>(type: "integer", nullable: false),
                    LandingPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExitPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Referrer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UtmSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmMedium = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmCampaign = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmContent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Gclid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Fbclid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Msclkid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Browser = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BrowserVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Os = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OsVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ViewportW = table.Column<int>(type: "integer", nullable: true),
                    ViewportH = table.Column<int>(type: "integer", nullable: true),
                    IsBot = table.Column<bool>(type: "boolean", nullable: false),
                    BotReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsentState = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Converted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionCount = table.Column<int>(type: "integer", nullable: false),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VisitorHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 23, 29, 29, 835, DateTimeKind.Utc).AddTicks(5760));

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsBot",
                table: "Sessions",
                column: "IsBot");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartedAt",
                table: "Sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_VisitorId",
                table: "Sessions",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_ConvertedAt",
                table: "Visitors",
                column: "ConvertedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_LastSeen",
                table: "Visitors",
                column: "LastSeen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Visitors");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 4, 40, 31, 709, DateTimeKind.Utc).AddTicks(3030));
        }
    }
}
