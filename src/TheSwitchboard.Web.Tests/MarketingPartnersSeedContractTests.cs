using System.Text.RegularExpressions;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Contract: the marketing-partners seed file must not contain near-duplicate
/// entries that differ ONLY by corporate suffix (LLC / Inc. / Co / Corp /
/// Group / Holdings / Company). When a buyer submits "Acme" or "Acme, LLC"
/// they are the same company — keeping both is defensive on a form but
/// visually clutters the public list and inflates the count.
///
/// SCOPE — intentionally conservative. We deliberately do NOT collapse:
///   - "Insurance" / "Ins." suffix variants — these may be distinct DBAs
///   - Spelling / typo variants like "Visqua" vs "Visiqua"
///   - Mojibake (already fixed in-place)
/// </summary>
public class MarketingPartnersSeedContractTests
{
    private static readonly Regex SuffixRe = new(
        @"(?:\s+|,\s*)(?:L\.?L\.?C\.?|Inc\.?|Co\.?|Corp\.?|Corporation|Ltd\.?|LP|L\.P\.|Limited|Company|Group|Holdings)\s*\.?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TheSwitchboardWeb.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("solution root not found");
        return dir.FullName;
    }

    /// <summary>Strip trailing corporate suffixes iteratively (handles "X, Inc., LLC").</summary>
    public static string StripSuffix(string s)
    {
        string prev;
        do
        {
            prev = s;
            s = SuffixRe.Replace(s, "").TrimEnd(' ', ',', '.');
        } while (prev != s);
        return s;
    }

    [Fact]
    public void Seed_HasNoCorporateSuffixNearDuplicates()
    {
        var path = Path.Combine(GetSolutionRoot(), "src", "TheSwitchboard.Web", "Data", "Seeds", "marketing-partners.txt");
        Assert.True(File.Exists(path), $"seed file not found: {path}");

        var lines = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in lines)
        {
            var key = Regex.Replace(StripSuffix(n), @"\s+", " ").Trim().ToLowerInvariant();
            if (key.Length == 0) continue;
            if (!groups.TryGetValue(key, out var bucket)) groups[key] = bucket = new List<string>();
            bucket.Add(n);
        }

        var dupGroups = groups.Values.Where(g => g.Count > 1).ToList();
        Assert.True(
            dupGroups.Count == 0,
            $"Seed has {dupGroups.Count} corporate-suffix near-duplicate groups. " +
            $"Examples: {string.Join(" | ", dupGroups.Take(5).Select(g => "[" + string.Join(", ", g) + "]"))}. " +
            "Collapse each group to the longest-form entry via a one-off dedup pass.");
    }
}
