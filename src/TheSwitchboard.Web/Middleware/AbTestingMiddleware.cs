using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Ab;

namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// For every public GET request, looks up active Experiments and ensures each one has a
/// sticky variant assignment for this visitor. Assignment is stored in a cookie named
/// <c>sw_ab_{slug}</c> and persisted to the AbAssignments table for conversion analysis.
///
/// Pipeline position: AFTER authentication (harmless if admin — admins get assigned too
/// for preview), BEFORE Razor Pages routing so HttpContext.Items["AB_&lt;slug&gt;"] is
/// available to PublicPageModel.
/// </summary>
public class AbTestingMiddleware
{
    private readonly RequestDelegate _next;

    public AbTestingMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/api/") && !path.StartsWith("/health") &&
                !path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/_"))
            {
                try
                {
                    var experiments = await db.Experiments.Where(e => e.IsActive).ToListAsync();
                    foreach (var exp in experiments)
                    {
                        var cookieName = $"sw_ab_{exp.Slug}";
                        if (context.Request.Cookies.TryGetValue(cookieName, out var existing) &&
                            !string.IsNullOrWhiteSpace(existing))
                        {
                            context.Items[$"AB_{exp.Slug}"] = existing;
                            continue;
                        }

                        var variants = await db.Variants.Where(v => v.ExperimentId == exp.Id).ToListAsync();
                        if (variants.Count == 0) continue;

                        var pick = WeightedPick(variants, Random.Shared);
                        context.Response.Cookies.Append(cookieName, pick.Name, new CookieOptions
                        {
                            HttpOnly = false,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = false,
                            MaxAge = TimeSpan.FromDays(30),
                            Path = "/"
                        });
                        context.Items[$"AB_{exp.Slug}"] = pick.Name;

                        // Persist assignment for analytics (best-effort; swallow on failure).
                        var visitorKey = context.Request.Cookies["sw_sid"] ?? Guid.NewGuid().ToString("N")[..16];
                        db.AbAssignments.Add(new AbAssignment
                        {
                            VisitorKey = visitorKey,
                            ExperimentId = exp.Id,
                            VariantId = pick.Id
                        });
                        try { await db.SaveChangesAsync(); } catch { /* ignore */ }
                    }
                }
                catch { /* A/B must never break the site */ }
            }
        }
        await _next(context);
    }

    private static Variant WeightedPick(List<Variant> variants, Random rng)
    {
        var total = variants.Sum(v => Math.Max(1, v.TrafficWeight));
        var roll = rng.Next(0, total);
        var acc = 0;
        foreach (var v in variants)
        {
            acc += Math.Max(1, v.TrafficWeight);
            if (roll < acc) return v;
        }
        return variants.Last();
    }
}
