using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSwitchboard.Web.Migrations
{
    /// <summary>
    /// H-7.I-05: Indexes on the hot-path query columns.
    ///
    ///   FormSubmissions(CreatedAt)           — admin time-range queries
    ///   FormSubmissions(PhoenixSyncStatus)   — retry worker scans Failed rows
    ///   AnalyticsEvents(Timestamp)           — dashboard time-range queries (aliased as CreatedAt in model docs)
    ///   AnalyticsEvents(Path)                — "top pages" dashboard card
    ///
    /// Note: FormSubmissions has no Email column — submitter email is stored
    /// inside the JSON Data field, so no index possible without a migration
    /// to promote it to a first-class column. Deferred.
    ///
    /// Safe to apply to a live DB — CreateIndex is near-instant at current
    /// row counts. At higher scale, rerun with CONCURRENTLY in a manual
    /// migration outside EF.
    /// </summary>
    public partial class H7_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_CreatedAt",
                table: "FormSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_PhoenixSyncStatus",
                table: "FormSubmissions",
                column: "PhoenixSyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_Timestamp",
                table: "AnalyticsEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_Path",
                table: "AnalyticsEvents",
                column: "Path");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AnalyticsEvents_Path",              table: "AnalyticsEvents");
            migrationBuilder.DropIndex(name: "IX_AnalyticsEvents_Timestamp",         table: "AnalyticsEvents");
            migrationBuilder.DropIndex(name: "IX_FormSubmissions_PhoenixSyncStatus", table: "FormSubmissions");
            migrationBuilder.DropIndex(name: "IX_FormSubmissions_CreatedAt",         table: "FormSubmissions");
        }
    }
}
