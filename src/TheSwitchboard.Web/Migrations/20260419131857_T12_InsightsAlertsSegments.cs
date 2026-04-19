using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T12_InsightsAlertsSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    FiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Context = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MetricExpression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Comparison = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    Window = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ChannelTarget = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Insights",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metric = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Current = table.Column<double>(type: "double precision", nullable: false),
                    Baseline = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Segments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Filter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segments", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 13, 18, 57, 23, DateTimeKind.Utc).AddTicks(8270));

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_FiredAt",
                table: "AlertLogs",
                column: "FiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Name",
                table: "AlertRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Insights_CreatedAt",
                table: "Insights",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Segments_Name",
                table: "Segments",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertLogs");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "Insights");

            migrationBuilder.DropTable(
                name: "Segments");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 13, 16, 32, 384, DateTimeKind.Utc).AddTicks(5040));
        }
    }
}
