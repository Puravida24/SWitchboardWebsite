using Microsoft.Playwright;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// End-to-end contact form behavior under a real browser. Supersedes the
/// previously-skipped S2-09 / S2-10 placeholders.
///
/// S2-09  Submit → /api/contact 200  → form hides, Phoenix vignette plays.
/// S2-10  Submit → /api/contact 500  → inline error, form stays editable,
///                                     submit button re-enables.
/// </summary>
[Trait("Category", "Playwright")]
[Collection("Playwright")]
public class PlaywrightContactFormTests
{
    private readonly PlaywrightFixture _fx;
    public PlaywrightContactFormTests(PlaywrightFixture fx) { _fx = fx; }

    private static async Task FillContactFormAsync(IPage page)
    {
        await page.FillAsync("#cf-name",    "Playwright Tester");
        await page.FillAsync("#cf-email",   "playwright@test.local");
        await page.FillAsync("#cf-company", "Switchboard QA");
        await page.SelectOptionAsync("#cf-role", new SelectOptionValue { Label = "Carrier" });
        await page.FillAsync("#cf-message", "automated end-to-end smoke");
    }

    // ------------------------------------------------------------------
    // S2-09: happy-path — real POST to /api/contact returns 200, form
    // hides, Phoenix vignette plays.
    // ------------------------------------------------------------------
    [Fact]
    public async Task S2_09_ContactForm_OnServer200_PlaysVignette()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync(_fx.BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await FillContactFormAsync(page);

            // Wait for the POST to finish alongside the click so we don't race.
            var responseTask = page.WaitForResponseAsync(r => r.Url.EndsWith("/api/contact") && r.Status == 200);
            await page.ClickAsync("button.form-submit");
            var res = await responseTask;
            Assert.Equal(200, res.Status);

            var vignette = page.Locator("#phoenixVignette");
            await Assertions.Expect(vignette).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bactive\b"));
            await Assertions.Expect(vignette).ToHaveAttributeAsync("aria-hidden", "false");

            // Contact form is hidden via inline style="display:none" once the 200 fires.
            var formDisplay = await page.EvalOnSelectorAsync<string>("#contactForm", "el => el.style.display");
            Assert.Equal("none", formDisplay);
        }
        finally { await page.CloseAsync(); }
    }

    // ------------------------------------------------------------------
    // S2-10: when the server returns 500 the form must stay visible and
    // re-enable the submit button so the user can retry.
    // ------------------------------------------------------------------
    [Fact]
    public async Task S2_10_ContactForm_OnServer500_StaysEditable()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            // Intercept /api/contact and force a 500 before we submit.
            await page.RouteAsync("**/api/contact", async route =>
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 500,
                    ContentType = "application/json",
                    Body = "{\"title\":\"forced-500\"}",
                });
            });

            // The page's submit handler calls `alert(...)` on failure. Accept it.
            string? alertMessage = null;
            page.Dialog += async (_, dialog) =>
            {
                alertMessage = dialog.Message;
                await dialog.AcceptAsync();
            };

            await page.GotoAsync(_fx.BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await FillContactFormAsync(page);
            await page.ClickAsync("button.form-submit");

            // Give the async handler + dialog a tick to fire.
            await page.WaitForTimeoutAsync(300);

            Assert.False(string.IsNullOrEmpty(alertMessage), "alert() was not fired on 500 — user got no feedback.");
            Assert.Contains("forced-500", alertMessage,
                StringComparison.OrdinalIgnoreCase);

            // Form stays visible (no display:none was set).
            var formDisplay = await page.EvalOnSelectorAsync<string>("#contactForm", "el => el.style.display");
            Assert.NotEqual("none", formDisplay);

            // Submit button is re-enabled so the user can retry.
            var disabled = await page.EvalOnSelectorAsync<bool>("button.form-submit", "el => el.disabled");
            Assert.False(disabled, "submit button is still disabled after 500 — user is trapped.");
        }
        finally { await page.CloseAsync(); }
    }
}
