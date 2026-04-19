using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T6_VitalsErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JsErrors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StackRedacted = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Line = table.Column<int>(type: "integer", nullable: true),
                    Col = table.Column<int>(type: "integer", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuildId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsErrors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebVitalSamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metric = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Rating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NavigationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NavId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebVitalSamples", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 1, 14, 46, 409, DateTimeKind.Utc).AddTicks(4430));

            migrationBuilder.CreateIndex(
                name: "IX_JsErrors_Fingerprint",
                table: "JsErrors",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_JsErrors_SessionId_Fingerprint",
                table: "JsErrors",
                columns: new[] { "SessionId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JsErrors_Ts",
                table: "JsErrors",
                column: "Ts");

            migrationBuilder.CreateIndex(
                name: "IX_WebVitalSamples_Path_Metric",
                table: "WebVitalSamples",
                columns: new[] { "Path", "Metric" });

            migrationBuilder.CreateIndex(
                name: "IX_WebVitalSamples_Ts",
                table: "WebVitalSamples",
                column: "Ts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JsErrors");

            migrationBuilder.DropTable(
                name: "WebVitalSamples");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 59, 51, 524, DateTimeKind.Utc).AddTicks(7170));
        }
    }
}
