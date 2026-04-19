namespace TheSwitchboard.Web.Tests;

/// <summary>
/// A1 RED test. This MUST fail to compile until the Playwright harness exists.
/// Harness contract:
///   - <see cref="PlaywrightFixture"/> boots the app under Kestrel on a real port
///   - Exposes <c>BaseUrl</c> (http://127.0.0.1:PORT) + <c>NewPageAsync()</c>
///   - Headless Chromium auto-installs on first run
///
/// Acceptance: this test passes when a real browser loads the homepage and
/// reads "Switchboard" from the <title>. Anything weaker (a file-grep, an
/// HttpClient fetch) is not a real browser test and won't catch JS/CSS bugs.
/// </summary>
[Trait("Category", "Playwright")]
public class PlaywrightSmokeTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fx;
    public PlaywrightSmokeTests(PlaywrightFixture fx) { _fx = fx; }

    [Fact]
    public async Task A1_HeadlessChromium_LoadsHomepage_AndReadsTitle()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            var response = await page.GotoAsync(_fx.BaseUrl + "/", new()
            {
                WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });

            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Homepage responded {response.Status}");

            var title = await page.TitleAsync();
            Assert.Contains("Switchboard", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
