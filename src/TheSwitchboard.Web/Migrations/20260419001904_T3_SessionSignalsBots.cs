using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T3_SessionSignalsBots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrowserSignals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ColorDepth = table.Column<int>(type: "integer", nullable: true),
                    HardwareConcurrency = table.Column<int>(type: "integer", nullable: true),
                    DeviceMemory = table.Column<int>(type: "integer", nullable: true),
                    TouchPoints = table.Column<int>(type: "integer", nullable: true),
                    ScreenW = table.Column<int>(type: "integer", nullable: true),
                    ScreenH = table.Column<int>(type: "integer", nullable: true),
                    PixelRatio = table.Column<double>(type: "double precision", nullable: true),
                    Cookies = table.Column<bool>(type: "boolean", nullable: true),
                    LocalStorage = table.Column<bool>(type: "boolean", nullable: true),
                    SessionStorage = table.Column<bool>(type: "boolean", nullable: true),
                    IsMetaWebview = table.Column<bool>(type: "boolean", nullable: true),
                    IsTikTokWebview = table.Column<bool>(type: "boolean", nullable: true),
                    CanvasFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WebGLVendor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WebGLRenderer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Battery = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Connection = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownProxyAsns",
                columns: table => new
                {
                    Asn = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownProxyAsns", x => x.Asn);
                });

            migrationBuilder.InsertData(
                table: "KnownProxyAsns",
                columns: new[] { "Asn", "Category", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 8075, "datacenter", "Microsoft Azure", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9009, "vpn", "M247 (VPN backbone)", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14061, "datacenter", "DigitalOcean", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15169, "datacenter", "Google Cloud", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16276, "datacenter", "OVH", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16509, "datacenter", "Amazon AWS", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20473, "datacenter", "Vultr", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24940, "datacenter", "Hetzner", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 42708, "vpn", "Portlane (ExpressVPN)", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 60068, "vpn", "CDN77 / Datacamp", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 63949, "datacenter", "Linode", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 202425, "vpn", "IP Volume (NordVPN)", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 212238, "vpn", "Datacamp (CyberGhost)", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 19, 3, 982, DateTimeKind.Utc).AddTicks(3530));

            migrationBuilder.CreateIndex(
                name: "IX_BrowserSignals_CanvasFingerprint",
                table: "BrowserSignals",
                column: "CanvasFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserSignals_SessionId",
                table: "BrowserSignals",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnownProxyAsns_Category",
                table: "KnownProxyAsns",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrowserSignals");

            migrationBuilder.DropTable(
                name: "KnownProxyAsns");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 3, 2, 341, DateTimeKind.Utc).AddTicks(6190));
        }
    }
}
