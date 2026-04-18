using System.Text.RegularExpressions;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Copy / legal-content contract tests. These assert the ground-truth state of
/// customer-facing text. They're file-read assertions (no HTTP boot), so they
/// run fast and deterministically.
///
/// Slice H-1 gates (CopyContractTests.cs — 2026-04-18):
///   A. Terms.html has no DMCA clause (dmca@ was never a real mailbox).
///   B. Accessibility page cites WCAG 2.2 consistently across body + meta tags.
///   C. Privacy + Terms both show "Effective April 17, 2026 · Last updated April 17, 2026".
///   D. Homepage does not repeat the "Every line of code, every data pipeline, every model" refrain.
///   E. security.txt uses legal@theswitchboardmarketing.com (the real mailbox), not security@.
/// </summary>
public class CopyContractTests
{
    private static string WebRoot()
    {
        // Tests run from bin/Debug/net9.0 — walk up to the repo.
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir!, "src", "TheSwitchboard.Web", "wwwroot");
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(WebRoot(), relative));

    private static string ReadSource(string relative)
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
            dir = Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(dir!, "src", "TheSwitchboard.Web", relative));
    }

    [Fact]
    public void H1_A_Terms_Has_No_DMCA_Clause()
    {
        var terms = Read("wireframes/terms.html");
        Assert.DoesNotContain("DMCA", terms);
        Assert.DoesNotContain("dmca@", terms, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("designated agent", terms, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void H1_B_Accessibility_Uses_Wcag_2_2_Consistently()
    {
        var a11y = Read("wireframes/accessibility.html");
        // No lingering 2.1 references.
        Assert.DoesNotContain("WCAG 2.1", a11y);
        Assert.DoesNotContain("2.1 AA", a11y);
        Assert.DoesNotContain("2.1 Level AA", a11y);
        // Must cite 2.2 at least once in body copy.
        Assert.Contains("WCAG 2.2", a11y);
    }

    [Fact]
    public void H1_C_Privacy_And_Terms_Dates_Are_EffectiveApril17()
    {
        var privacy = Read("wireframes/privacy.html");
        var terms = Read("wireframes/terms.html");
        var expected = "Effective April 17, 2026 &middot; Last updated April 17, 2026";
        Assert.Contains(expected, privacy);
        Assert.Contains(expected, terms);
        // The stale April 16 must not linger anywhere in either legal page.
        Assert.DoesNotContain("April 16, 2026", privacy);
        Assert.DoesNotContain("April 16, 2026", terms);
    }

    [Fact]
    public void H1_D_Homepage_EveryRefrain_DoesNotRepeatVerbatim()
    {
        var homepage = Read("wireframes/design-32e-newsprint.html");
        // The refrain "Every line of code" should appear at most once in rendered body copy.
        // Count case-insensitive occurrences outside HTML comments.
        var visibleBody = Regex.Replace(homepage, @"<!--.*?-->", "", RegexOptions.Singleline);
        var count = Regex.Matches(visibleBody, @"Every line of code", RegexOptions.IgnoreCase).Count;
        Assert.True(count <= 1,
            $"Expected 'Every line of code' to appear at most once, found {count} times.");
    }

    [Fact]
    public void H1_E_SecurityTxt_UsesLegalEmail_NotSecurityEmail()
    {
        var seo = ReadSource("Api/SeoEndpoints.cs");
        Assert.Contains("mailto:legal@theswitchboardmarketing.com", seo);
        Assert.DoesNotContain("mailto:security@theswitchboardmarketing.com", seo);
    }
}
