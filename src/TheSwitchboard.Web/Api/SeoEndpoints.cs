using System.Text;

namespace TheSwitchboard.Web.Api;

/// <summary>
/// SEO / crawler surfaces: /sitemap.xml, /robots.txt, /llms.txt, /.well-known/security.txt.
/// </summary>
public static class SeoEndpoints
{
    private record SitemapEntry(string Loc, string Priority, string ChangeFreq);

    private static readonly SitemapEntry[] PublicRoutes =
    {
        new("/",              "1.0", "weekly"),
        new("/privacy",       "0.4", "yearly"),
        new("/terms",         "0.4", "yearly"),
        new("/accessibility", "0.4", "yearly"),
    };

    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", (HttpContext ctx) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");
            foreach (var r in PublicRoutes)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{r.Loc}</loc>");
                sb.AppendLine($"    <lastmod>{today}</lastmod>");
                sb.AppendLine($"    <changefreq>{r.ChangeFreq}</changefreq>");
                sb.AppendLine($"    <priority>{r.Priority}</priority>");
                if (r.Loc == "/")
                {
                    sb.AppendLine("    <image:image>");
                    sb.AppendLine($"      <image:loc>{baseUrl}/wireframes/assets/logo/switchboard-logo.png</image:loc>");
                    sb.AppendLine("      <image:title>The Switchboard logo</image:title>");
                    sb.AppendLine("    </image:image>");
                }
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

        // RFC 9116 security.txt — where researchers can report vulnerabilities.
        // Registered on both /.well-known/... (canonical) and /security.txt (legacy).
        string SecurityTxt(HttpContext ctx)
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var expires = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            return $"""
                Contact: mailto:security@theswitchboardmarketing.com
                Expires: {expires}
                Preferred-Languages: en
                Canonical: {baseUrl}/.well-known/security.txt
                Policy: {baseUrl}/security
                """;
        }
        app.MapGet("/.well-known/security.txt", (HttpContext ctx) => Results.Content(SecurityTxt(ctx), "text/plain"));
        app.MapGet("/security.txt", (HttpContext ctx) => Results.Content(SecurityTxt(ctx), "text/plain"));
    }
}
