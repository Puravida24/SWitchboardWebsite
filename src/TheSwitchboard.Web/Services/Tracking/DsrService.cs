using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// CCPA / GDPR "delete-my-data" workflow. Purges every table that could hold
/// a trace of the subject, logs per-table row counts to DataSubjectRequest so
/// counsel can show proof of erasure.
/// </summary>
public interface IDsrService
{
    Task<DsrResult> DeleteByEmailAsync(string email);
}

public sealed record DsrResult(long RequestId, IDictionary<string, int> DeletedRowCounts);

public class DsrService : IDsrService
{
    private readonly AppDbContext _db;
    public DsrService(AppDbContext db) { _db = db; }

    public async Task<DsrResult> DeleteByEmailAsync(string email)
    {
        var now = DateTime.UtcNow;
        var emailLower = email.Trim().ToLowerInvariant();
        var emailHash = Sha256Hex(emailLower);
        var counts = new Dictionary<string, int>();

        // 1. FormSubmissions — match by JSON substring.
        var submissions = await _db.FormSubmissions
            .Where(s => s.Data.Contains(emailLower) || s.Data.Contains(email))
            .ToListAsync();
        counts["FormSubmissions"] = submissions.Count;
        var sessionIds = new HashSet<string>();

        // 2. ConsentCertificates — match by EmailHash.
        var certs = await _db.ConsentCertificates
            .Where(c => c.EmailHash == emailHash || submissions.Select(s => s.ConsentCertificateId).Contains(c.Id))
            .ToListAsync();
        counts["ConsentCertificates"] = certs.Count;
        foreach (var c in certs.Where(c => !string.IsNullOrEmpty(c.SessionId))) sessionIds.Add(c.SessionId!);

        // 3. Sessions (and everything keyed off them).
        var visitorIds = (await _db.Sessions
            .Where(s => sessionIds.Contains(s.Id) && s.VisitorId != null)
            .Select(s => s.VisitorId!)
            .ToListAsync()).ToHashSet();

        var clicks = _db.ClickEvents.Where(c => sessionIds.Contains(c.SessionId));
        var scrolls = _db.ScrollSamples.Where(s => sessionIds.Contains(s.SessionId));
        var mouse = _db.MouseTrails.Where(m => sessionIds.Contains(m.SessionId));
        var forms = _db.FormInteractions.Where(f => sessionIds.Contains(f.SessionId));
        var vitals = _db.WebVitalSamples.Where(v => sessionIds.Contains(v.SessionId));
        var errors = _db.JsErrors.Where(e => sessionIds.Contains(e.SessionId));
        var signals = _db.BrowserSignals.Where(b => sessionIds.Contains(b.SessionId));
        var pvs = _db.PageViews.Where(p => p.SessionId != null && sessionIds.Contains(p.SessionId!));

        counts["ClickEvents"] = await clicks.CountAsync();
        counts["ScrollSamples"] = await scrolls.CountAsync();
        counts["MouseTrails"] = await mouse.CountAsync();
        counts["FormInteractions"] = await forms.CountAsync();
        counts["WebVitalSamples"] = await vitals.CountAsync();
        counts["JsErrors"] = await errors.CountAsync();
        counts["BrowserSignals"] = await signals.CountAsync();
        counts["PageViews"] = await pvs.CountAsync();

        _db.ClickEvents.RemoveRange(clicks);
        _db.ScrollSamples.RemoveRange(scrolls);
        _db.MouseTrails.RemoveRange(mouse);
        _db.FormInteractions.RemoveRange(forms);
        _db.WebVitalSamples.RemoveRange(vitals);
        _db.JsErrors.RemoveRange(errors);
        _db.BrowserSignals.RemoveRange(signals);
        _db.PageViews.RemoveRange(pvs);

        // 4. Replays for those sessions.
        var replays = await _db.Replays.Where(r => sessionIds.Contains(r.SessionId)).ToListAsync();
        counts["ReplayChunks"] = await _db.ReplayChunks.CountAsync(c => replays.Select(r => r.Id).Contains(c.ReplayId));
        _db.ReplayChunks.RemoveRange(_db.ReplayChunks.Where(c => replays.Select(r => r.Id).Contains(c.ReplayId)));
        counts["Replays"] = replays.Count;
        _db.Replays.RemoveRange(replays);

        // 5. Sessions + Visitors.
        var sessionRows = _db.Sessions.Where(s => sessionIds.Contains(s.Id));
        counts["Sessions"] = await sessionRows.CountAsync();
        _db.Sessions.RemoveRange(sessionRows);

        var visitorRows = _db.Visitors.Where(v => visitorIds.Contains(v.Id));
        counts["Visitors"] = await visitorRows.CountAsync();
        _db.Visitors.RemoveRange(visitorRows);

        // 6. Finally drop certs + submissions.
        _db.ConsentCertificates.RemoveRange(certs);
        _db.FormSubmissions.RemoveRange(submissions);

        // 7. Audit row.
        var request = new DataSubjectRequest
        {
            Email = email,
            RequestedAt = now,
            FulfilledAt = now,
            Status = "complete",
            DeletedRowCounts = System.Text.Json.JsonSerializer.Serialize(counts)
        };
        _db.DataSubjectRequests.Add(request);

        await _db.SaveChangesAsync();
        return new DsrResult(request.Id, counts);
    }

    private static string Sha256Hex(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
