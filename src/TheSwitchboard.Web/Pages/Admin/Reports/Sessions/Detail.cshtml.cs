using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Sessions;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;
    public DetailModel(AppDbContext db) { _db = db; }

    public Session? Session { get; private set; }
    public Replay? Replay { get; private set; }
    public int PageViewCount { get; private set; }
    public int ClickCount { get; private set; }
    public int FormEventCount { get; private set; }
    public int ErrorCount { get; private set; }
    public DateTime? SubmitMomentUtc { get; private set; }
    public IReadOnlyList<TimelineItem> Timeline { get; private set; } = Array.Empty<TimelineItem>();

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        Session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (Session is null) return NotFound();

        Replay = await _db.Replays.FirstOrDefaultAsync(r => r.SessionId == id);
        PageViewCount = await _db.PageViews.CountAsync(p => p.SessionId == id);
        ClickCount = await _db.ClickEvents.CountAsync(c => c.SessionId == id);
        FormEventCount = await _db.FormInteractions.CountAsync(f => f.SessionId == id);
        ErrorCount = await _db.JsErrors.CountAsync(e => e.SessionId == id);

        var submit = await _db.FormInteractions
            .Where(f => f.SessionId == id && f.Event == "submit")
            .OrderBy(f => f.OccurredAt)
            .FirstOrDefaultAsync();
        SubmitMomentUtc = submit?.OccurredAt;

        var items = new List<TimelineItem>();
        var pvs = await _db.PageViews.Where(p => p.SessionId == id).OrderBy(p => p.Timestamp).ToListAsync();
        items.AddRange(pvs.Select(p => new TimelineItem(p.Timestamp, "pageview", p.Path, null)));
        var clicks = await _db.ClickEvents.Where(c => c.SessionId == id).OrderBy(c => c.Ts).ToListAsync();
        items.AddRange(clicks.Select(c => new TimelineItem(c.Ts, c.IsRage ? "rage-click" : c.IsDead ? "dead-click" : "click", c.Path, c.Selector)));
        var forms = await _db.FormInteractions.Where(f => f.SessionId == id).OrderBy(f => f.OccurredAt).ToListAsync();
        items.AddRange(forms.Select(f => new TimelineItem(f.OccurredAt, "form:" + f.Event, f.Path, f.FieldName)));
        var errs = await _db.JsErrors.Where(e => e.SessionId == id).OrderBy(e => e.Ts).ToListAsync();
        items.AddRange(errs.Select(e => new TimelineItem(e.Ts, "error", e.Path, e.Message)));
        Timeline = items.OrderBy(x => x.Ts).ToList();

        return Page();
    }

    public async Task<IActionResult> OnGetChunksAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var replay = await _db.Replays.FirstOrDefaultAsync(r => r.SessionId == id);
        if (replay is null) return NotFound();
        var chunks = await _db.ReplayChunks
            .Where(c => c.ReplayId == replay.Id)
            .OrderBy(c => c.Sequence)
            .Select(c => new
            {
                c.Sequence,
                c.Ts,
                payloadBase64 = Convert.ToBase64String(c.Payload)
            })
            .ToListAsync();
        return new JsonResult(new { compressed = replay.Compressed, chunks });
    }

    public sealed record TimelineItem(DateTime Ts, string Kind, string Path, string? Label);
}
