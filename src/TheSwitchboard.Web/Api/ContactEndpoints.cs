using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using TheSwitchboard.Web.Models.Forms;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Api;

public static class ContactEndpoints
{
    // H-07.3 / H-3.A: Origin-check CSRF defense for unauthenticated JSON APIs.
    // If the request carries an Origin header (every browser POST does) it
    // must match the request's own Host. Non-browser clients (curl, server-
    // to-server) typically omit Origin — those are allowed through.
    private static bool IsOriginAllowed(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (string.IsNullOrEmpty(origin)) return true;
        var host = context.Request.Host.Value;
        var scheme = context.Request.Scheme;
        return string.Equals(origin, $"{scheme}://{host}", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(origin, $"https://{host}",  StringComparison.OrdinalIgnoreCase) ||
               string.Equals(origin, $"http://{host}",   StringComparison.OrdinalIgnoreCase);
    }

    public static void MapContactApi(this WebApplication app)
    {
        app.MapPost("/api/contact", async (
            ContactFormRequest request,
            IValidator<ContactFormRequest> validator,
            IFormService formService,
            TheSwitchboard.Web.Data.AppDbContext db,
            TheSwitchboard.Web.Services.Tracking.IGoalService goals,
            HttpContext context) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            // S2-07: honeypot — CSS-hidden field. A human can't fill it.
            if (!string.IsNullOrWhiteSpace(request.Website))
            {
                return Results.Ok(new { success = true });
            }

            // S2-08: submit-timing — a human can't fill + submit a form in under two seconds.
            // Absent LoadedAt is fine (graceful degradation for no-JS clients).
            if (request.LoadedAt is DateTime loaded)
            {
                var delta = DateTime.UtcNow - loaded.ToUniversalTime();
                if (delta.TotalSeconds < 2)
                {
                    return Results.Ok(new { success = true });
                }
            }

            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var ip = context.Connection.RemoteIpAddress?.ToString();
            var ua = context.Request.Headers.UserAgent.FirstOrDefault();
            var submission = await formService.ProcessContactAsync(request, ip, ua, "/");

            // T-7B: link the most recent unlinked consent cert for this session
            // back to the form submission. Done server-side so the client doesn't
            // need to await the /consent response before submitting the form.
            var sid = context.Request.Cookies["sw_sid"];
            if (!string.IsNullOrWhiteSpace(sid))
            {
                var cert = await db.ConsentCertificates
                    .Where(c => c.SessionId == sid && c.FormSubmissionId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                if (cert is not null)
                {
                    cert.FormSubmissionId = submission.Id;
                    var sub = await db.FormSubmissions.FindAsync(submission.Id);
                    if (sub is not null) sub.ConsentCertificateId = cert.Id;
                    await db.SaveChangesAsync();
                }
            }

            // T-11 goal evaluation.
            try { await goals.EvaluateFormSubmissionAsync(submission, sid, context.Request.Cookies["sw_vid"]); }
            catch { /* never break the contact path */ }

            return Results.Ok(new { success = true, id = submission.Id });
        });

        app.MapPost("/api/demo", async (
            DemoBookingRequest request,
            IValidator<DemoBookingRequest> validator,
            IFormService formService,
            HttpContext context) =>
        {
            // H-3.A: Origin-check CSRF defense (same as /api/contact).
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var data = new Dictionary<string, string>
            {
                ["firstName"] = request.FirstName,
                ["lastName"] = request.LastName,
                ["email"] = request.Email,
                ["phone"] = request.Phone ?? "",
                ["companyName"] = request.CompanyName ?? "",
                ["selectedDate"] = request.SelectedDate.ToString("yyyy-MM-dd"),
                ["selectedTime"] = request.SelectedTime,
                ["timezone"] = request.Timezone ?? "America/Los_Angeles"
            };

            var ip = context.Connection.RemoteIpAddress?.ToString();
            var ua = context.Request.Headers.UserAgent.FirstOrDefault();
            var submission = await formService.ProcessSubmissionAsync("demo", data, ip, ua, "/demo");
            return Results.Ok(new { success = true, id = submission.Id });
        });

        app.MapPost("/api/analytics/event", async (
            AnalyticsEventRequest request,
            IAnalyticsService analyticsService) =>
        {
            await analyticsService.RecordEventAsync(
                request.Name, request.Category, request.Label,
                request.Value, request.Path, request.SessionId, request.Metadata);
            return Results.Ok();
        });

        // H-3.B: SES bounce webhook with HMAC-SHA256 signature verification.
        // Sender must compute HMAC-SHA256(body, Ses:WebhookSecret) and pass it as
        // "X-SES-Signature: sha256=<hexlower>". Without a configured secret we reject
        // all inbound calls — fail-closed is safer than fail-open for a prod webhook.
        app.MapPost("/api/ses/bounce", async (HttpContext ctx, IFormService formService, IConfiguration config) =>
        {
            var secret = config["Ses:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
                return Results.Unauthorized();

            ctx.Request.EnableBuffering();
            string body;
            using (var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true))
                body = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0;

            var provided = ctx.Request.Headers["X-SES-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(provided) || !provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();
            var providedHex = provided["sha256=".Length..];

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

            // Timing-safe equality — compare byte arrays, not strings.
            var expectedBytes = Convert.FromHexString(computed);
            byte[] providedBytes;
            try { providedBytes = Convert.FromHexString(providedHex); }
            catch { return Results.Unauthorized(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
                return Results.Unauthorized();

            SesBouncePayload? payload;
            try { payload = JsonSerializer.Deserialize<SesBouncePayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { return Results.BadRequest(); }
            if (payload is null || string.IsNullOrWhiteSpace(payload.Email))
                return Results.BadRequest();

            await formService.MarkEmailBouncedAsync(payload.Email);
            return Results.Ok(new { success = true });
        });

        // S2-21: admin-only Phoenix test ping.
        app.MapPost("/Admin/Phoenix/Test", async (IPhoenixCrmService crm) =>
        {
            var ok = await crm.SendFormSubmissionAsync("test", new Dictionary<string, string>
            {
                ["name"] = "Admin Test",
                ["email"] = "admin-test@switchboard.local",
                ["sentAt"] = DateTime.UtcNow.ToString("o")
            });
            return Results.Ok(new { success = ok });
        }).RequireAuthorization();
    }
}

public record SesBouncePayload(string Email);

public record AnalyticsEventRequest(
    string Name,
    string? Category = null,
    string? Label = null,
    string? Value = null,
    string? Path = null,
    string? SessionId = null,
    string? Metadata = null);
