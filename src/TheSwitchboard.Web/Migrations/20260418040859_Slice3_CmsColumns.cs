using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class Slice3_CmsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CtaDeck",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaHeadline",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialBodyLeft",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialBodyRight",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialDeck",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialHeadline",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialKicker",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterCopyright",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterStamp",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroDeck",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroHeadline",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PullQuote",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PullQuoteAttribution",
                table: "SiteSettings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityKey = table.Column<string>(type: "text", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalPages",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalPages", x => x.Slug);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CtaDeck", "CtaHeadline", "EditorialBodyLeft", "EditorialBodyRight", "EditorialDeck", "EditorialHeadline", "EditorialKicker", "FooterCopyright", "FooterStamp", "HeroDeck", "HeroHeadline", "PullQuote", "PullQuoteAttribution", "UpdatedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null, new DateTime(2026, 4, 18, 4, 8, 59, 611, DateTimeKind.Utc).AddTicks(860) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentVersions");

            migrationBuilder.DropTable(
                name: "LegalPages");

            migrationBuilder.DropColumn(
                name: "CtaDeck",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CtaHeadline",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EditorialBodyLeft",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EditorialBodyRight",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EditorialDeck",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EditorialHeadline",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EditorialKicker",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterCopyright",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterStamp",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroDeck",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroHeadline",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PullQuote",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PullQuoteAttribution",
                table: "SiteSettings");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 3, 40, 34, 895, DateTimeKind.Utc).AddTicks(5610));
        }
    }
}
