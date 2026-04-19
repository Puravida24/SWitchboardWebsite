using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Drives /Admin/Reports/Compliance + /Admin/Reports/Certificates + /Admin/Reports/Disclosures.
/// Capture-rate math, bot-rate math, disclosure-version lifecycle.
/// </summary>
public interface IComplianceAnalyticsService
{
    Task<ComplianceReport> GetAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<ConsentCertificate>> ListCertsAsync(DateTime fromUtc, DateTime toUtc, string? status, int skip, int take);
    Task<ConsentCertificate?> GetCertAsync(string certificateId);
    Task<IReadOnlyList<DisclosureVersion>> ListVersionsAsync();
    Task<IDictionary<long, int>> CertCountByVersionAsync();
    Task RegisterVersionAsync(long versionId, string? actor);
    Task RetireVersionAsync(long versionId, string? actor);
}

public sealed record ComplianceReport(
    int TotalSubmissions,
    int CertifiedSubmissions,
    int CaptureRatePct,
    int PhoneProvidingSubmissions,
    int PhoneCertifiedSubmissions,
    int PhoneCaptureRatePct,
    int TotalCerts,
    int SuspiciousBotCerts,
    int BotRatePct,
    int UnregisteredActiveVersions);

public class ComplianceAnalyticsService : IComplianceAnalyticsService
{
    private readonly AppDbContext _db;
    public ComplianceAnalyticsService(AppDbContext db) { _db = db; }

    public async Task<ComplianceReport> GetAsync(DateTime fromUtc, DateTime toUtc)
    {
        var submissions = await _db.FormSubmissions
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .Select(s => new { s.Id, s.ConsentCertificateId, s.Data })
            .ToListAsync();

        var total = submissions.Count;
        var certified = submissions.Count(s => s.ConsentCertificateId.HasValue);
        // Phone-providing heuristic: form Data contains a non-empty "phone" field.
        // Uses simple substring — good enough for capture-rate slicing, not an exfil.
        var phoneProviding = submissions.Count(s =>
            !string.IsNullOrEmpty(s.Data) &&
            s.Data.Contains("\"phone\":\"", StringComparison.OrdinalIgnoreCase) &&
            !s.Data.Contains("\"phone\":\"\"", StringComparison.OrdinalIgnoreCase));
        var phoneCertified = submissions.Count(s =>
            s.ConsentCertificateId.HasValue &&
            !string.IsNullOrEmpty(s.Data) &&
            s.Data.Contains("\"phone\":\"", StringComparison.OrdinalIgnoreCase) &&
            !s.Data.Contains("\"phone\":\"\"", StringComparison.OrdinalIgnoreCase));

        var totalCerts = await _db.ConsentCertificates
            .CountAsync(c => c.CreatedAt >= fromUtc && c.CreatedAt <= toUtc);
        var bots = await _db.ConsentCertificates
            .CountAsync(c => c.CreatedAt >= fromUtc && c.CreatedAt <= toUtc && c.IsSuspiciousBot);
        var activeVersionIds = await _db.ConsentCertificates
            .Where(c => c.CreatedAt >= fromUtc && c.CreatedAt <= toUtc && c.DisclosureVersionId != null)
            .Select(c => c.DisclosureVersionId!.Value)
            .Distinct()
            .ToListAsync();
        var unregistered = await _db.DisclosureVersions
            .Where(v => activeVersionIds.Contains(v.Id) && v.Status != "registered")
            .CountAsync();

        return new ComplianceReport(
            TotalSubmissions: total,
            CertifiedSubmissions: certified,
            CaptureRatePct: total == 0 ? 0 : (int)Math.Round(100.0 * certified / total),
            PhoneProvidingSubmissions: phoneProviding,
            PhoneCertifiedSubmissions: phoneCertified,
            PhoneCaptureRatePct: phoneProviding == 0 ? 0 : (int)Math.Round(100.0 * phoneCertified / phoneProviding),
            TotalCerts: totalCerts,
            SuspiciousBotCerts: bots,
            BotRatePct: totalCerts == 0 ? 0 : (int)Math.Round(100.0 * bots / totalCerts),
            UnregisteredActiveVersions: unregistered);
    }

    public async Task<IReadOnlyList<ConsentCertificate>> ListCertsAsync(DateTime fromUtc, DateTime toUtc, string? status, int skip, int take)
    {
        var q = _db.ConsentCertificates
            .Where(c => c.CreatedAt >= fromUtc && c.CreatedAt <= toUtc)
            .AsQueryable();
        if (status == "bot") q = q.Where(c => c.IsSuspiciousBot);
        if (status == "human") q = q.Where(c => !c.IsSuspiciousBot);
        if (status == "expired") q = q.Where(c => c.ExpiresAt < DateTime.UtcNow);
        return await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ConsentCertificate?> GetCertAsync(string certificateId)
    {
        return await _db.ConsentCertificates.FirstOrDefaultAsync(c => c.CertificateId == certificateId);
    }

    public async Task<IReadOnlyList<DisclosureVersion>> ListVersionsAsync()
    {
        return await _db.DisclosureVersions.OrderByDescending(v => v.EffectiveFrom).ToListAsync();
    }

    public async Task<IDictionary<long, int>> CertCountByVersionAsync()
    {
        var rows = await _db.ConsentCertificates
            .Where(c => c.DisclosureVersionId != null)
            .GroupBy(c => c.DisclosureVersionId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();
        return rows.ToDictionary(r => r.Id, r => r.Count);
    }

    public async Task RegisterVersionAsync(long versionId, string? actor)
    {
        var v = await _db.DisclosureVersions.FindAsync(versionId);
        if (v is null) return;
        v.Status = "registered";
        v.CreatedBy = actor;
        await _db.SaveChangesAsync();
    }

    public async Task RetireVersionAsync(long versionId, string? actor)
    {
        var v = await _db.DisclosureVersions.FindAsync(versionId);
        if (v is null) return;
        v.Status = "retired";
        v.EffectiveTo = DateTime.UtcNow;
        v.CreatedBy = actor;
        await _db.SaveChangesAsync();
    }
}
