using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T5_ScrollMouseForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormInteractions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VisitorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FormId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Event = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DwellMs = table.Column<int>(type: "integer", nullable: true),
                    CharCount = table.Column<int>(type: "integer", nullable: true),
                    CorrectionCount = table.Column<int>(type: "integer", nullable: true),
                    PastedFlag = table.Column<bool>(type: "boolean", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormInteractions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MouseTrails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    ViewportW = table.Column<int>(type: "integer", nullable: false),
                    ViewportH = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouseTrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrollSamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    MaxDepth = table.Column<int>(type: "integer", nullable: false),
                    ViewportH = table.Column<int>(type: "integer", nullable: false),
                    DocumentH = table.Column<int>(type: "integer", nullable: false),
                    TimeSinceLoadMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrollSamples", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 59, 51, 524, DateTimeKind.Utc).AddTicks(7170));

            migrationBuilder.CreateIndex(
                name: "IX_FormInteractions_Event",
                table: "FormInteractions",
                column: "Event");

            migrationBuilder.CreateIndex(
                name: "IX_FormInteractions_FormId_FieldName",
                table: "FormInteractions",
                columns: new[] { "FormId", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_FormInteractions_SessionId_FormId",
                table: "FormInteractions",
                columns: new[] { "SessionId", "FormId" });

            migrationBuilder.CreateIndex(
                name: "IX_MouseTrails_Path_Ts",
                table: "MouseTrails",
                columns: new[] { "Path", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_MouseTrails_SessionId",
                table: "MouseTrails",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrollSamples_Path_Depth",
                table: "ScrollSamples",
                columns: new[] { "Path", "Depth" });

            migrationBuilder.CreateIndex(
                name: "IX_ScrollSamples_SessionId_Path_Depth",
                table: "ScrollSamples",
                columns: new[] { "SessionId", "Path", "Depth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormInteractions");

            migrationBuilder.DropTable(
                name: "MouseTrails");

            migrationBuilder.DropTable(
                name: "ScrollSamples");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 39, 55, 584, DateTimeKind.Utc).AddTicks(2140));
        }
    }
}
