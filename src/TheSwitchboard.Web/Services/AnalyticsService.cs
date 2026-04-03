using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;

namespace TheSwitchboard.Web.Services;

public interface IAnalyticsService
{
    Task RecordPageViewAsync(string path, string? referrer, string? userAgent, string? ipAddress, string? sessionId);
    Task RecordEventAsync(string name, string? category, string? label, string? value, string? path, string? sessionId, string? metadata);
    Task<int> GetPageViewCountAsync(DateTime from, DateTime to);
    Task<List<(string Path, int Count)>> GetTopPagesAsync(DateTime from, DateTime to, int limit = 10);
    Task<int> GetUniqueVisitorCountAsync(DateTime from, DateTime to);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecordPageViewAsync(string path, string? referrer, string? userAgent, string? ipAddress, string? sessionId)
    {
        var ipHash = ipAddress != null ? HashIp(ipAddress) : null;

        var pageView = new PageView
        {
            Path = path,
            Referrer = referrer,
            UserAgent = userAgent,
            IpHash = ipHash,
            SessionId = sessionId
        };

        _db.PageViews.Add(pageView);
        await _db.SaveChangesAsync();
    }

    public async Task RecordEventAsync(string name, string? category, string? label, string? value, string? path, string? sessionId, string? metadata)
    {
        var ev = new AnalyticsEvent
        {
            Name = name,
            Category = category,
            Label = label,
            Value = value,
            Path = path,
            SessionId = sessionId,
            Metadata = metadata
        };

        _db.AnalyticsEvents.Add(ev);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetPageViewCountAsync(DateTime from, DateTime to)
    {
        return await _db.PageViews
            .CountAsync(p => p.Timestamp >= from && p.Timestamp <= to);
    }

    public async Task<List<(string Path, int Count)>> GetTopPagesAsync(DateTime from, DateTime to, int limit = 10)
    {
        var results = await _db.PageViews
            .Where(p => p.Timestamp >= from && p.Timestamp <= to)
            .GroupBy(p => p.Path)
            .Select(g => new { Path = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.Path, r.Count)).ToList();
    }

    public async Task<int> GetUniqueVisitorCountAsync(DateTime from, DateTime to)
    {
        return await _db.PageViews
            .Where(p => p.Timestamp >= from && p.Timestamp <= to && p.IpHash != null)
            .Select(p => p.IpHash)
            .Distinct()
            .CountAsync();
    }

    private static string HashIp(string ip)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip + "switchboard-salt"));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
