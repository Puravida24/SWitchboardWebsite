using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Api;

/// <summary>
/// Phoenix dial-time consent verification. Phoenix posts <c>{certificateId, email, phone}</c>
/// with a bearer token; server SHA-256 hashes the email+phone the same way the cert was
/// minted and returns <c>{match, matchedFields, certificateExpired, disclosureVersion,
/// consentTimestamp}</c>. Canonicalization: lowercase + trim before hashing.
///
/// This is the TCPA defense: "we verified consent at dial time against a tamper-evident
/// proof record, hashed PII never left your system, and we can produce the cert."
/// </summary>
public static class ConsentMatchEndpoints
{
    public sealed class MatchRequest
    {
        public string? CertificateId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public static void MapConsentMatchEndpoints(this WebApplication app)
    {
        app.MapPost("/api/consent/match", async (
            MatchRequest? request,
            HttpContext context,
            AppDbContext db,
            IConfiguration config,
            ILogger<ConsentMatchMarker> logger) =>
        {
            var configured = config["PhoenixCrm:ConsentApiKey"];
            if (string.IsNullOrEmpty(configured))
                return Results.Problem("Phoenix consent key not configured", statusCode: 503);

            var auth = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.Ordinal))
                return Results.Unauthorized();
            var presented = auth["Bearer ".Length..].Trim();
            // Constant-time compare so brute force can't be timed.
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(presented),
                    Encoding.UTF8.GetBytes(configured)))
                return Results.Unauthorized();

            if (request is null || string.IsNullOrWhiteSpace(request.CertificateId))
                return Results.BadRequest(new { error = "certificateId required" });

            var cert = await db.ConsentCertificates
                .FirstOrDefaultAsync(c => c.CertificateId == request.CertificateId);
            if (cert is null) return Results.NotFound(new { error = "certificate not found" });
            if (cert.ExpiresAt < DateTime.UtcNow)
                return Results.StatusCode(StatusCodes.Status410Gone);

            var matchedFields = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !string.IsNullOrEmpty(cert.EmailHash) &&
                FixedTimeHashEquals(cert.EmailHash, Sha256Hex(request.Email!)))
            {
                matchedFields.Add("email");
            }
            if (!string.IsNullOrWhiteSpace(request.Phone) &&
                !string.IsNullOrEmpty(cert.PhoneHash) &&
                FixedTimeHashEquals(cert.PhoneHash, Sha256Hex(request.Phone!)))
            {
                matchedFields.Add("phone");
            }

            DisclosureVersionDto? version = null;
            if (cert.DisclosureVersionId is long vid)
            {
                var v = await db.DisclosureVersions.FindAsync(vid);
                if (v is not null) version = new DisclosureVersionDto(v.Version, v.Status);
            }

            logger.LogInformation(
                "Phoenix consent match cert={Cert} match={Match} fields={Fields}",
                cert.CertificateId, matchedFields.Count > 0, string.Join(",", matchedFields));

            return Results.Json(new
            {
                match = matchedFields.Count > 0,
                matchedFields,
                certificateExpired = false,
                consentTimestamp = cert.ConsentTimestamp,
                disclosureVersion = version
            });
        });
    }

    private static string Sha256Hex(string input)
    {
        var canonical = input.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool FixedTimeHashEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }

    public sealed record DisclosureVersionDto(string Version, string Status);

    /// <summary>ILogger category marker.</summary>
    public sealed class ConsentMatchMarker { }
}
