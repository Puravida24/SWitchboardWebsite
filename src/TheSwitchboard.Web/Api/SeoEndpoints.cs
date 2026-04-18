using System.Text;

namespace TheSwitchboard.Web.Api;

/// <summary>
/// Slice 4 SEO surfaces: /sitemap.xml, /robots.txt, /llms.txt.
/// Route collection is hardcoded (small set of public pages, matches VERTICAL_SLICE_PLAN).
/// </summary>
public static class SeoEndpoints
{
    private static readonly string[] PublicRoutes = { "/", "/privacy", "/terms", "/accessibility" };

    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", (HttpContext ctx) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var route in PublicRoutes)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{route}</loc>");
                sb.AppendLine($"    <changefreq>weekly</changefreq>");
                sb.AppendLine("  </url>");
            }
            sb.AppendLine("</urlset>");
            return Results.Content(sb.ToString(), "application/xml");
        });

        app.MapGet("/robots.txt", (HttpContext ctx) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var body = $"""
                User-agent: *
                Allow: /
                Disallow: /Admin
                Disallow: /admin
                Disallow: /api

                Sitemap: {baseUrl}/sitemap.xml
                """;
            return Results.Content(body, "text/plain");
        });

        app.MapGet("/llms.txt", () =>
        {
            var body = """
                # The Switchboard

                > Insurance intelligence platform. Enrich, score, and route every inbound insurance record in under five milliseconds. Written by people who have written policies and bought the clicks.

                ## What we do

                The Switchboard (platform name: Phoenix) is the real-time intelligence layer that sits between insurance demand sources and underwriters. We enrich every inbound record with firmographic, demographic, and behavioral signal; score it against proprietary models grounded in real insurance outcomes; and route it to the right desk.

                ## Lines of business

                - Auto (personal lines, fleet, commercial auto, motorcycle)
                - Home (homeowners, HO3/HO5, condo, dwelling, flood, umbrella)
                - Commercial (small business through mid-market, NAICS class coded)

                ## Public pages

                - /                — Homepage: product overview, live Phoenix telemetry, contact form.
                - /privacy          — Privacy policy.
                - /terms            — Terms of service.
                - /accessibility    — Accessibility statement.

                ## Contact

                The Switchboard, LLC · Orem, Utah · theswitchboardmarketing.com
                """;
            return Results.Content(body, "text/plain");
        });
    }
}
