using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T7B_ConsentCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ConsentCertificateId",
                table: "FormSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsentCertificates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FormSubmissionId = table.Column<int>(type: "integer", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ConsentTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentMethod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ConsentElementSelector = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClickX = table.Column<int>(type: "integer", nullable: true),
                    ClickY = table.Column<int>(type: "integer", nullable: true),
                    PageLoadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeOnPageSeconds = table.Column<int>(type: "integer", nullable: true),
                    DisclosureText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DisclosureTextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisclosureVersionId = table.Column<long>(type: "bigint", nullable: true),
                    DisclosureFontSize = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DisclosureColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DisclosureBackgroundColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DisclosureContrastRatio = table.Column<double>(type: "double precision", nullable: true),
                    DisclosureIsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BrowserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OsName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ScreenResolution = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ViewportW = table.Column<int>(type: "integer", nullable: true),
                    ViewportH = table.Column<int>(type: "integer", nullable: true),
                    PageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KeystrokesPerMinute = table.Column<int>(type: "integer", nullable: true),
                    FormFieldsInteracted = table.Column<int>(type: "integer", nullable: true),
                    MouseDistancePx = table.Column<int>(type: "integer", nullable: true),
                    ScrollDepthPercent = table.Column<int>(type: "integer", nullable: true),
                    IsSuspiciousBot = table.Column<bool>(type: "boolean", nullable: false),
                    EmailHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PhoneHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisclosureVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FullText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisclosureVersions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 1, 36, 6, 897, DateTimeKind.Utc).AddTicks(3550));

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_CertificateId",
                table: "ConsentCertificates",
                column: "CertificateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_DisclosureTextHash",
                table: "ConsentCertificates",
                column: "DisclosureTextHash");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_EmailHash",
                table: "ConsentCertificates",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_ExpiresAt",
                table: "ConsentCertificates",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_FormSubmissionId",
                table: "ConsentCertificates",
                column: "FormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentCertificates_PhoneHash",
                table: "ConsentCertificates",
                column: "PhoneHash");

            migrationBuilder.CreateIndex(
                name: "IX_DisclosureVersions_Status",
                table: "DisclosureVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisclosureVersions_TextHash",
                table: "DisclosureVersions",
                column: "TextHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisclosureVersions_Version",
                table: "DisclosureVersions",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentCertificates");

            migrationBuilder.DropTable(
                name: "DisclosureVersions");

            migrationBuilder.DropColumn(
                name: "ConsentCertificateId",
                table: "FormSubmissions");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 1, 27, 29, 9, DateTimeKind.Utc).AddTicks(4650));
        }
    }
}
