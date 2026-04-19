using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class T11_GoalsDeploysDsr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeletedRowCounts = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeployChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Author = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeployChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalConversions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GoalId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VisitorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: true),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalConversions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchExpression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Value = table.Column<int>(type: "integer", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 13, 10, 49, 760, DateTimeKind.Utc).AddTicks(8130));

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_RequestedAt",
                table: "DataSubjectRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Status",
                table: "DataSubjectRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeployChanges_DeployedAt",
                table: "DeployChanges",
                column: "DeployedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GoalConversions_GoalId_Ts",
                table: "GoalConversions",
                columns: new[] { "GoalId", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_Name",
                table: "Goals",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSubjectRequests");

            migrationBuilder.DropTable(
                name: "DeployChanges");

            migrationBuilder.DropTable(
                name: "GoalConversions");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 19, 13, 4, 8, 805, DateTimeKind.Utc).AddTicks(1360));
        }
    }
}
