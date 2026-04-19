using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// CSV export streaming for the /Admin/Reports/Exports download center.
/// Writes header + rows to the supplied stream without buffering the whole
/// set in memory — large exports stay flat on RAM.
/// </summary>
public interface IExportService
{
    Task<int> WriteSessionsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output);
    Task<int> WriteFormSubmissionsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output);
    Task<int> WriteClicksCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output);
    Task<int> WriteJsErrorsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output);
    Task<int> WriteVitalsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output);
}

public class ExportService : IExportService
{
    private readonly AppDbContext _db;
    public ExportService(AppDbContext db) { _db = db; }

    private static async Task WriteLineAsync(StreamWriter w, params object?[] cells)
    {
        var parts = new string[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            var s = cells[i]?.ToString() ?? "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            {
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            parts[i] = s;
        }
        await w.WriteLineAsync(string.Join(',', parts));
    }

    public async Task<int> WriteSessionsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output)
    {
        using var w = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await WriteLineAsync(w, "SessionId", "VisitorId", "StartedAt", "EndedAt", "DurationSeconds", "PageCount",
            "LandingPath", "UtmSource", "UtmCampaign", "DeviceType", "Browser", "Os", "IsBot", "BotReason", "Converted");
        var rows = await _db.Sessions
            .Where(s => s.StartedAt >= fromUtc && s.StartedAt <= toUtc)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();
        foreach (var s in rows)
        {
            await WriteLineAsync(w,
                s.Id, s.VisitorId, s.StartedAt.ToString("O"), s.EndedAt.ToString("O"), s.DurationSeconds, s.PageCount,
                s.LandingPath, s.UtmSource, s.UtmCampaign, s.DeviceType, s.Browser, s.Os, s.IsBot, s.BotReason, s.Converted);
        }
        await w.FlushAsync();
        return rows.Count;
    }

    public async Task<int> WriteFormSubmissionsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output)
    {
        using var w = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await WriteLineAsync(w, "Id", "CreatedAt", "FormType", "Role", "SourcePage", "ConsentCertificateId", "PhoenixSyncStatus");
        var rows = await _db.FormSubmissions
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
        foreach (var s in rows)
        {
            await WriteLineAsync(w, s.Id, s.CreatedAt.ToString("O"), s.FormType, s.Role, s.SourcePage, s.ConsentCertificateId, s.PhoenixSyncStatus);
        }
        await w.FlushAsync();
        return rows.Count;
    }

    public async Task<int> WriteClicksCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output)
    {
        using var w = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await WriteLineAsync(w, "SessionId", "Path", "Ts", "X", "Y", "Selector", "TagName", "ElementText", "IsRage", "IsDead");
        var rows = await _db.ClickEvents
            .Where(c => c.Ts >= fromUtc && c.Ts <= toUtc)
            .OrderBy(c => c.Ts)
            .ToListAsync();
        foreach (var c in rows)
        {
            await WriteLineAsync(w, c.SessionId, c.Path, c.Ts.ToString("O"), c.X, c.Y, c.Selector, c.TagName, c.ElementText, c.IsRage, c.IsDead);
        }
        await w.FlushAsync();
        return rows.Count;
    }

    public async Task<int> WriteJsErrorsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output)
    {
        using var w = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await WriteLineAsync(w, "SessionId", "Path", "Ts", "Message", "Source", "Line", "Fingerprint", "Count");
        var rows = await _db.JsErrors
            .Where(e => e.Ts >= fromUtc && e.Ts <= toUtc)
            .OrderBy(e => e.Ts)
            .ToListAsync();
        foreach (var e in rows)
        {
            await WriteLineAsync(w, e.SessionId, e.Path, e.Ts.ToString("O"), e.Message, e.Source, e.Line, e.Fingerprint, e.Count);
        }
        await w.FlushAsync();
        return rows.Count;
    }

    public async Task<int> WriteVitalsCsvAsync(DateTime fromUtc, DateTime toUtc, Stream output)
    {
        using var w = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await WriteLineAsync(w, "SessionId", "Path", "Ts", "Metric", "Value", "Rating", "NavigationType");
        var rows = await _db.WebVitalSamples
            .Where(v => v.Ts >= fromUtc && v.Ts <= toUtc)
            .OrderBy(v => v.Ts)
            .ToListAsync();
        foreach (var v in rows)
        {
            await WriteLineAsync(w, v.SessionId, v.Path, v.Ts.ToString("O"), v.Metric, v.Value, v.Rating, v.NavigationType);
        }
        await w.FlushAsync();
        return rows.Count;
    }
}
