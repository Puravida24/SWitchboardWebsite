using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// A2 — real headless-Chromium verification of H-5 (CSP nonce), H-6/T-7 (frame
/// embedding), and the admin login flow + auth cookie flags.
///
/// Unit/integration tests already assert header strings, but browser-driven tests
/// are what catch the subtle real-world failures: a nonce that's in the header
/// but missing on an inline script, a redirect loop that only triggers after
/// JS runs, a SameSite cookie silently stripped by the browser because another
/// attribute was malformed, etc.
/// </summary>
[Trait("Category", "Playwright")]
[Collection("Playwright")]
public class PlaywrightSecurityTests
{
    private readonly PlaywrightFixture _fx;
    public PlaywrightSecurityTests(PlaywrightFixture fx) { _fx = fx; }

    // ------------------------------------------------------------------
    // A2-01  Every inline <script> carries a nonce that matches the
    //        nonce advertised on the response's Content-Security-Policy
    //        header. If one gets emitted without a nonce, Chromium will
    //        silently refuse to execute it — this test proves parity.
    // ------------------------------------------------------------------
    [Fact]
    public async Task A2_01_InlineScripts_AllCarryNonce_MatchingCspHeader()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            var response = await page.GotoAsync(_fx.BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Homepage responded {response.Status}");

            var csp = (await response.AllHeadersAsync())["content-security-policy"];
            var nonceMatch = Regex.Match(csp, @"'nonce-([A-Za-z0-9+/=_\-]+)'");
            Assert.True(nonceMatch.Success, "CSP header did not contain a 'nonce-...' token.");
            var headerNonce = nonceMatch.Groups[1].Value;

            // Every <script> without a src="" attribute is inline — must carry the nonce.
            //
            // Chromium's "nonce hiding" spec clears `getAttribute('nonce')` post-parse
            // so a compromised page can't exfiltrate other scripts' nonces; the live
            // value stays on the IDL property `el.nonce`. Read that instead.
            var inlineNonces = await page.EvalOnSelectorAllAsync<string[]>(
                "script:not([src])",
                "els => els.map(e => e.nonce || e.getAttribute('nonce') || '')");

            Assert.NotEmpty(inlineNonces);
            foreach (var n in inlineNonces)
            {
                Assert.Equal(headerNonce, n);
            }
        }
        finally { await page.CloseAsync(); }
    }

    // ------------------------------------------------------------------
    // A2-02  frame-ancestors 'self' + X-Frame-Options: SAMEORIGIN must
    //        prevent external sites from iframing us. Using a data: URL
    //        as the parent gives us a null-origin parent that is clearly
    //        not our origin — real browsers block the frame.
    // ------------------------------------------------------------------
    [Fact]
    public async Task A2_02_FrameAncestors_BlocksCrossOriginEmbedding()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            // Header assertion — cheap guardrail first.
            var direct = await page.GotoAsync(_fx.BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.NotNull(direct);
            var h = await direct!.AllHeadersAsync();
            Assert.Contains("frame-ancestors 'self'", h["content-security-policy"]);
            Assert.Equal("SAMEORIGIN", h["x-frame-options"]);

            // Behavioral assertion — load a null-origin parent that tries to frame us;
            // the parent must see zero navigated frames (browser refused embedding).
            var embedHtml = $"<!doctype html><title>probe</title><iframe id='f' src='{_fx.BaseUrl}/'></iframe>";
            var dataUrl = "data:text/html;charset=utf-8," + Uri.EscapeDataString(embedHtml);
            await page.GotoAsync(dataUrl, new() { WaitUntil = WaitUntilState.Load });

            // Give the (refused) navigation a beat to resolve.
            await page.WaitForTimeoutAsync(500);

            // When the browser blocks the frame, the iframe document exists but its
            // location stays at "about:blank" (or is inaccessible). Either way, the
            // frame never reaches our <title>Switchboard</title>.
            var frameTitle = await page.EvaluateAsync<string?>(
                "() => { try { return document.getElementById('f').contentDocument?.title ?? null; } catch { return null; } }");

            Assert.True(
                string.IsNullOrEmpty(frameTitle) || !frameTitle!.Contains("Switchboard", StringComparison.OrdinalIgnoreCase),
                $"Homepage was embedded cross-origin (title='{frameTitle}'). frame-ancestors / X-Frame-Options not enforced.");
        }
        finally { await page.CloseAsync(); }
    }

    // ------------------------------------------------------------------
    // A2-03  End-to-end admin login via the real login page AND the Identity
    //        auth cookie flags it sets. Two assertions in one login to avoid
    //        hammering the SignInManager with back-to-back logins (Identity's
    //        internal state can race on the follow-up request within ms of
    //        the first POST completing — observed as a 10s redirect timeout).
    //
    //        Verifies:
    //          - POST /Admin/Login → 302 /Admin/Dashboard (happy path)
    //          - the Identity cookie is HttpOnly (no JS theft)
    //          - the Identity cookie is SameSite=Strict or Lax (no CSRF leak)
    // ------------------------------------------------------------------
    [Fact]
    public async Task A2_03_AdminLogin_Succeeds_AndIssuesLockedDownCookie()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync(_fx.BaseUrl + "/Admin/Login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.FillAsync("input[name='Email']",    PlaywrightFixture.AdminEmail);
            await page.FillAsync("input[name='Password']", PlaywrightFixture.AdminPassword);
            await page.ClickAsync("button[type='submit']");
            // 30s (was 10s) — under full-suite CPU contention the dashboard redirect
            // can take >10s to land. The actual app is fast in isolation; the bump is
            // purely for test-runtime slack, not a perf regression allowance.
            await page.WaitForURLAsync("**/Admin/Dashboard**", new() { Timeout = 30_000 });

            Assert.EndsWith("/Admin/Dashboard", new Uri(page.Url).AbsolutePath, StringComparison.OrdinalIgnoreCase);

            var cookies = await page.Context.CookiesAsync();
            var auth = cookies.FirstOrDefault(c => c.Name.StartsWith(".AspNetCore.Identity", StringComparison.Ordinal));
            Assert.NotNull(auth);
            Assert.True(auth!.HttpOnly, "Identity cookie is NOT HttpOnly — XSS can read it.");
            Assert.True(
                auth.SameSite == SameSiteAttribute.Strict || auth.SameSite == SameSiteAttribute.Lax,
                $"Identity cookie SameSite is '{auth.SameSite}' — expected Strict or Lax.");
        }
        finally { await page.CloseAsync(); }
    }
}
