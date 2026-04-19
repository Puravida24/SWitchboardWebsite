using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T2_PageViewAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "PageViews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "PageViews",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Referrer",
                table: "PageViews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpHash",
                table: "PageViews",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "PageViews",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "PageViews",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "PageViews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fbclid",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gclid",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LandingFlag",
                table: "PageViews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Msclkid",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Os",
                table: "PageViews",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "PageViews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewportH",
                table: "PageViews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewportW",
                table: "PageViews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorId",
                table: "PageViews",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 0, 3, 2, 341, DateTimeKind.Utc).AddTicks(6190));

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_LandingFlag",
                table: "PageViews",
                column: "LandingFlag");

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_SessionId_Timestamp",
                table: "PageViews",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_UtmCampaign",
                table: "PageViews",
                column: "UtmCampaign");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PageViews_LandingFlag",
                table: "PageViews");

            migrationBuilder.DropIndex(
                name: "IX_PageViews_SessionId_Timestamp",
                table: "PageViews");

            migrationBuilder.DropIndex(
                name: "IX_PageViews_UtmCampaign",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "Fbclid",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "Gclid",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "LandingFlag",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "Msclkid",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "Os",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "ViewportH",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "ViewportW",
                table: "PageViews");

            migrationBuilder.DropColumn(
                name: "VisitorId",
                table: "PageViews");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "PageViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "PageViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Referrer",
                table: "PageViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpHash",
                table: "PageViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "PageViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 18, 23, 29, 29, 835, DateTimeKind.Utc).AddTicks(5760));
        }
    }
}
