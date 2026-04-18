using FluentValidation;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Api;

public static class ContactEndpoints
{
    public static void MapContactApi(this WebApplication app)
    {
        app.MapPost("/api/contact", async (
            ContactFormRequest request,
            IValidator<ContactFormRequest> validator,
            IFormService formService,
            HttpContext context) =>
        {
            // H-07.3: Origin-check CSRF defense for unauthenticated JSON APIs.
            // If the request carries an Origin header (every browser POST does) it
            // must match the request's own Host. Non-browser clients (curl, server-
            // to-server) typically omit Origin — those are allowed through.
            var origin = context.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrEmpty(origin))
            {
                var host = context.Request.Host.Value;
                var scheme = context.Request.Scheme;
                var allowed1 = $"{scheme}://{host}";
                var allowed2 = $"https://{host}";
                var allowed3 = $"http://{host}";
                if (!string.Equals(origin, allowed1, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(origin, allowed2, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(origin, allowed3, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }
            }

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

            return Results.Ok(new { success = true, id = submission.Id });
        });

        app.MapPost("/api/demo", async (
            DemoBookingRequest request,
            IValidator<DemoBookingRequest> validator,
            IFormService formService,
            HttpContext context) =>
        {
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

        // S2-22: SES bounce webhook — flags submissions whose email address bounced.
        // Minimal shape for now; Slice 3 will add signature verification + richer payload handling.
        app.MapPost("/api/ses/bounce", async (SesBouncePayload payload, IFormService formService) =>
        {
            if (string.IsNullOrWhiteSpace(payload.Email)) return Results.BadRequest();
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
