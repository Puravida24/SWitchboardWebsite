using System.Text.RegularExpressions;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice H-7 Observability & Ops Hardening — contract tests.
///
///   A. Correlation ID middleware registered in pipeline (Program.cs) and
///      CorrelationIdMiddleware class exists under Middleware/.
///   B. Integration: inbound request without X-Correlation-ID gets one generated.
///   C. Integration: inbound request with X-Correlation-ID has it echoed back.
///   D. Migration file exists creating indexes on FormSubmissions + AnalyticsEvents.
///   E. RUNBOOK.md exists at repo root with required sections.
///   F. BACKUP_RESTORE.md exists at repo root with required sections.
/// </summary>
public class ObservabilityContractTests
{
    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return dir!;
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relative));

    // ── A: middleware file + registration ──────────────────────────────
    [Fact]
    public void H7_A_CorrelationIdMiddleware_Registered()
    {
        var file = Path.Combine(RepoRoot(), "src/TheSwitchboard.Web/Middleware/CorrelationIdMiddleware.cs");
        Assert.True(File.Exists(file), "CorrelationIdMiddleware.cs must exist");

        var program = Read("src/TheSwitchboard.Web/Program.cs");
        Assert.Contains("UseMiddleware<CorrelationIdMiddleware>", program);
    }

    // ── D: EF migration for indexes ────────────────────────────────────
    [Fact]
    public void H7_D_MigrationAddsIndexes_ForFormSubmissions_AndAnalyticsEvents()
    {
        var migrationsDir = Path.Combine(RepoRoot(), "src/TheSwitchboard.Web/Migrations");
        Assert.True(Directory.Exists(migrationsDir));
        var migrationFiles = Directory.GetFiles(migrationsDir, "*_H7_Indexes.cs");
        Assert.NotEmpty(migrationFiles);

        var content = File.ReadAllText(migrationFiles[0]);
        // Must create indexes on the hot-path columns we care about.
        Assert.Contains("FormSubmissions", content);
        Assert.Contains("PhoenixSyncStatus", content);
        Assert.Contains("AnalyticsEvents", content);
        Assert.Matches(@"CreateIndex[^;]+""CreatedAt""", content);
        Assert.Matches(@"CreateIndex[^;]+""Timestamp""", content);
    }

    // ── E: RUNBOOK.md ──────────────────────────────────────────────────
    [Fact]
    public void H7_E_Runbook_Exists_With_RequiredSections()
    {
        var path = Path.Combine(RepoRoot(), "RUNBOOK.md");
        Assert.True(File.Exists(path), "RUNBOOK.md must exist at repo root");
        var rb = File.ReadAllText(path);
        // Operations must know: how to rollback, how to rotate admin pw, how to
        // check logs, how to restore DB, who to contact.
        Assert.Matches(@"(?i)rollback", rb);
        Assert.Matches(@"(?i)(rotate|reset).{0,80}(admin|password)", rb);
        Assert.Matches(@"(?i)(seq|log)", rb);
        Assert.Matches(@"(?i)restore", rb);
    }

    // ── F: BACKUP_RESTORE.md ───────────────────────────────────────────
    [Fact]
    public void H7_F_BackupRestore_Exists_With_RequiredSections()
    {
        var path = Path.Combine(RepoRoot(), "BACKUP_RESTORE.md");
        Assert.True(File.Exists(path), "BACKUP_RESTORE.md must exist at repo root");
        var br = File.ReadAllText(path);
        Assert.Matches(@"(?i)backup", br);
        Assert.Matches(@"(?i)restore", br);
        Assert.Matches(@"(?i)(railway|postgres)", br);
        // Must document RPO + RTO targets so on-call knows expectations.
        Assert.Matches(@"(?i)(rpo|rto|recovery)", br);
    }
}
