using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Sessions;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) { _db = db; }

    public const int PageSize = 50;

    public IReadOnlyList<Session> Rows { get; private set; } = Array.Empty<Session>();
    public int TotalCount { get; private set; }
    public int PageIndex { get; private set; } = 1;

    // Filters
    public string? Source { get; private set; }
    public string? Device { get; private set; }
    public string? Bot { get; private set; }       // "only" | "hide" | null
    public string? Converted { get; private set; } // "only" | "hide" | null

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Summary counters
    public int TotalSessions { get; private set; }
    public int BotSessions { get; private set; }
    public int HumanSessions { get; private set; }
    public int ConvertedSessions { get; private set; }

    public async Task OnGetAsync(int? page, string? source, string? device, string? bot, string? converted)
    {
        PageIndex = page is >= 1 ? page.Value : 1;
        Source = source; Device = device; Bot = bot; Converted = converted;

        var q = _db.Sessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(source)) q = q.Where(s => s.UtmSource == source);
        if (!string.IsNullOrWhiteSpace(device)) q = q.Where(s => s.DeviceType == device);
        if (bot == "only") q = q.Where(s => s.IsBot);
        if (bot == "hide") q = q.Where(s => !s.IsBot);
        if (converted == "only") q = q.Where(s => s.Converted);
        if (converted == "hide") q = q.Where(s => !s.Converted);

        TotalCount = await q.CountAsync();
        Rows = await q
            .OrderByDescending(s => s.StartedAt)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Summary is always across all sessions (not filtered) so the top tiles
        // are a stable ground-truth even if the user narrows the list below.
        TotalSessions = await _db.Sessions.CountAsync();
        BotSessions = await _db.Sessions.CountAsync(s => s.IsBot);
        HumanSessions = TotalSessions - BotSessions;
        ConvertedSessions = await _db.Sessions.CountAsync(s => s.Converted);
    }
}
