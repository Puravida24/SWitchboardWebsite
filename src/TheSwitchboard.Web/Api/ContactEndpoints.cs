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
                ["title"] = request.Title ?? "",
                ["companySize"] = request.CompanySize ?? "",
                ["insuranceLines"] = request.InsuranceLines ?? "",
                ["monthlyVolume"] = request.MonthlyVolume ?? "",
                ["biggestChallenge"] = request.BiggestChallenge ?? "",
                ["message"] = request.Message ?? "",
                ["tcpaConsent"] = request.TcpaConsent.ToString()
            };

            var ip = context.Connection.RemoteIpAddress?.ToString();
            var ua = context.Request.Headers.UserAgent.FirstOrDefault();

            var submission = await formService.ProcessSubmissionAsync("contact", data, ip, ua, "/contact");

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
    }
}

public record AnalyticsEventRequest(
    string Name,
    string? Category = null,
    string? Label = null,
    string? Value = null,
    string? Path = null,
    string? SessionId = null,
    string? Metadata = null);
