using System.Security.Cryptography;
using System.Text;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Api;

/// <summary>
/// Post-deploy webhook — CI calls this with a bearer token after a successful
/// Railway deploy so the admin Trends/ChangesLog can overlay a vertical marker.
/// </summary>
public static class OpsEndpoints
{
    public sealed class DeployChangeRequest
    {
        public string? Sha { get; set; }
        public string? Summary { get; set; }
        public string? Category { get; set; }
        public string? Author { get; set; }
        public DateTime? DeployedAt { get; set; }
    }

    public static void MapOpsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/ops/deploy-change", async (
            DeployChangeRequest? request,
            HttpContext context,
            AppDbContext db,
            IConfiguration config) =>
        {
            var configured = config["Ops:DeployChangeToken"];
            if (string.IsNullOrEmpty(configured))
                return Results.Problem("Deploy-change token not configured", statusCode: 503);

            var auth = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.Ordinal))
                return Results.Unauthorized();
            var presented = auth["Bearer ".Length..].Trim();
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(presented),
                    Encoding.UTF8.GetBytes(configured)))
                return Results.Unauthorized();

            if (request is null || string.IsNullOrWhiteSpace(request.Sha) || string.IsNullOrWhiteSpace(request.Summary))
                return Results.BadRequest(new { error = "sha + summary required" });

            var row = new DeployChange
            {
                Sha = request.Sha!,
                Summary = request.Summary!.Length > 500 ? request.Summary[..500] : request.Summary!,
                Category = request.Category,
                Author = request.Author,
                DeployedAt = request.DeployedAt ?? DateTime.UtcNow
            };
            db.DeployChanges.Add(row);
            await db.SaveChangesAsync();
            return Results.Created($"/Admin/Reports/ChangesLog#{row.Id}", new { id = row.Id });
        });
    }
}
