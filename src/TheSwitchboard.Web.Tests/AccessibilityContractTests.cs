using System.Text.RegularExpressions;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice H-5 Accessibility Polish — file-read contract tests.
///
///   A. design-32e.css must contain a @media (prefers-reduced-motion: reduce) block
///      that disables/reduces animations for users with vestibular disorders or
///      explicit OS 'Reduce motion' preferences.
///   B. Homepage must contain a visually-hidden Skip-to-Content link pointing at
///      #main — keyboard users can tab past the nav in one keystroke.
///   C. CSS must style .skip-link so it becomes visible when focused.
/// </summary>
public class AccessibilityContractTests
{
    private static string WebRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir!, "src", "TheSwitchboard.Web", "wwwroot");
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(WebRoot(), relative));

    [Fact]
    public void H5_A_Css_Honors_PrefersReducedMotion()
    {
        var css = Read("css/design-32e.css");
        // Must include a prefers-reduced-motion:reduce media query.
        Assert.Matches(@"@media[^{]*prefers-reduced-motion\s*:\s*reduce", css);
        // That block must neutralize animation/transition durations.
        // Minified CSS can vary — accept either !important explicit form or `animation:none`.
        Assert.Matches(@"prefers-reduced-motion[^{]*{[^}]*(animation-duration|animation:\s*none|transition-duration|transition:\s*none)",
            css);
    }

    [Fact]
    public void H5_B_Homepage_Has_SkipToContent_Link()
    {
        var home = Read("wireframes/design-32e-newsprint.html");
        // Skip link must point at #main (the <main id="main"> landmark added in the
        // Lighthouse fix) and carry the .skip-link class so CSS can hide+focus-reveal.
        Assert.Matches(@"<a[^>]+href\s*=\s*[""']#main[""'][^>]*class\s*=\s*[""'][^""']*skip-link",
            home);
    }

    [Fact]
    public void H5_C_Css_StylesSkipLinkFocusReveal()
    {
        var css = Read("css/design-32e.css");
        // The .skip-link class must be defined AND have a :focus variant so keyboard
        // users see it slide into view when tabbing.
        Assert.Contains(".skip-link", css);
        Assert.Matches(@"\.skip-link\s*:\s*focus", css);
    }

    // ── H-6.C — Logo is served as WebP with PNG fallback via <picture> ─────
    [Fact]
    public void H6_C_Logo_WebpFile_Exists_AndIsSmaller()
    {
        var pngPath  = Path.Combine(WebRoot(), "wireframes/assets/logo/switchboard-logo.png");
        var webpPath = Path.Combine(WebRoot(), "wireframes/assets/logo/switchboard-logo.webp");
        Assert.True(File.Exists(pngPath),  $"PNG logo missing at {pngPath}");
        Assert.True(File.Exists(webpPath), $"WebP logo missing at {webpPath}");
        var pngSize  = new FileInfo(pngPath).Length;
        var webpSize = new FileInfo(webpPath).Length;
        Assert.True(webpSize < pngSize,
            $"WebP ({webpSize} bytes) should be smaller than PNG ({pngSize} bytes)");
    }

    [Fact]
    public void H6_C_Homepage_UsesPictureElement_ForLogo()
    {
        var home = Read("wireframes/design-32e-newsprint.html");
        // Both logo instances (masthead + footer) must be wrapped in <picture>
        // with a <source srcset=...webp type="image/webp"> pointing at the WebP
        // variant and a fallback <img src=...png> for browsers that don't support WebP.
        Assert.Matches(@"<source[^>]+srcset=[""'][^""']*switchboard-logo\.webp[""'][^>]*type=[""']image/webp[""']",
            home);
    }
}
