using System.Text.Json;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Contract: every audit that <c>lighthouse:recommended</c> asserts as an error
/// against our 4 tested pages (home, /privacy, /terms, /accessibility) must have
/// an explicit handling in <c>.lighthouserc.json</c> — either "off", "warn", or a
/// tuned [level, options] override.
///
/// The universe of audits here is empirical: it's the union of every audit that
/// has failed at least one Lighthouse CI run on main. When CI surfaces a new one,
/// add it to <see cref="RequiredOverrides"/> and provide a handling in the config.
/// </summary>
public class LighthouseConfigContractTests
{
    /// <summary>
    /// Audits that must have an explicit override in .lighthouserc.json because
    /// lighthouse:recommended asserts them, but on our simple pages they either:
    /// (a) are skipped by collect (canonical, is-crawlable),
    /// (b) return NaN because the relevant condition doesn't apply
    ///     (lcp-lazy-loaded, non-composited-animations, prioritize-lcp-image),
    /// (c) fail on real issues we're not ready to block on yet
    ///     (color-contrast, errors-in-console, inspector-issues, bf-cache,
    ///      unminified-javascript, uses-responsive-images, unsized-images).
    /// </summary>
    public static IEnumerable<object[]> RequiredOverrides => new[]
    {
        // Meta / collect-skipped
        new object[] { "canonical" },
        new object[] { "is-crawlable" },
        // NaN on our pages
        new object[] { "lcp-lazy-loaded" },
        new object[] { "non-composited-animations" },
        new object[] { "prioritize-lcp-image" },
        // Real issues — demoted to warn until followed up
        new object[] { "bf-cache" },
        new object[] { "color-contrast" },
        new object[] { "errors-in-console" },
        new object[] { "inspector-issues" },
        new object[] { "unminified-javascript" },
        new object[] { "unsized-images" },
        new object[] { "uses-responsive-images" },
    };

    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TheSwitchboardWeb.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("solution root not found");
        return dir.FullName;
    }

    private static JsonElement LoadAssertions()
    {
        var path = Path.Combine(GetSolutionRoot(), ".lighthouserc.json");
        Assert.True(File.Exists(path), $".lighthouserc.json not found at {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .GetProperty("ci")
            .GetProperty("assert")
            .GetProperty("assertions")
            .Clone();
    }

    [Theory]
    [MemberData(nameof(RequiredOverrides))]
    public void Audit_HasExplicitOverride(string auditId)
    {
        var assertions = LoadAssertions();
        Assert.True(
            assertions.TryGetProperty(auditId, out var val),
            $"Expected explicit assertion override for '{auditId}' in .lighthouserc.json. " +
            "This audit has failed CI before and must have handling (\"off\", \"warn\", or a " +
            "[level, options] array). If you're seeing this fail, either the override was " +
            "removed or CI surfaced a new recurring failure that needs to be addressed.");

        bool ok = val.ValueKind switch
        {
            JsonValueKind.String when val.GetString() is "off" or "warn" => true,
            JsonValueKind.Array when val.GetArrayLength() >= 1 &&
                                    val[0].GetString() is "off" or "warn" or "error" => true,
            _ => false
        };
        Assert.True(ok,
            $"Override for '{auditId}' must be \"off\" / \"warn\" (string) or a [level, options] array.");
    }
}
