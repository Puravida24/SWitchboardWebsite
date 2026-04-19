using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Goal evaluation. Called from the form / pageview handlers when an event
/// might match a registered goal; writes a GoalConversion row per hit AND
/// flips Session.Converted + Visitor.ConvertedAt so reports reflect the win.
/// </summary>
public interface IGoalService
{
    Task EvaluateFormSubmissionAsync(FormSubmission submission, string? sessionId, string? visitorId);
    Task EvaluatePageViewAsync(string path, string? sessionId, string? visitorId);
}

public class GoalService : IGoalService
{
    private readonly AppDbContext _db;
    public GoalService(AppDbContext db) { _db = db; }

    public async Task EvaluateFormSubmissionAsync(FormSubmission submission, string? sessionId, string? visitorId)
    {
        var goals = await _db.Goals.Where(g => g.Enabled && g.Kind == "form").ToListAsync();
        var fired = 0;
        foreach (var g in goals)
        {
            var match = string.IsNullOrEmpty(g.MatchExpression) || submission.FormType == g.MatchExpression;
            if (!match) continue;
            _db.GoalConversions.Add(new GoalConversion
            {
                GoalId = g.Id,
                SessionId = sessionId,
                VisitorId = visitorId,
                Ts = DateTime.UtcNow,
                Value = g.Value,
                Path = submission.SourcePage
            });
            fired++;
        }
        if (fired > 0) await FlagConvertedAsync(sessionId, visitorId);
        await _db.SaveChangesAsync();
    }

    public async Task EvaluatePageViewAsync(string path, string? sessionId, string? visitorId)
    {
        var goals = await _db.Goals.Where(g => g.Enabled && g.Kind == "pageview").ToListAsync();
        var fired = 0;
        foreach (var g in goals)
        {
            var match = !string.IsNullOrEmpty(g.MatchExpression) && path == g.MatchExpression;
            if (!match) continue;
            _db.GoalConversions.Add(new GoalConversion
            {
                GoalId = g.Id,
                SessionId = sessionId,
                VisitorId = visitorId,
                Ts = DateTime.UtcNow,
                Value = g.Value,
                Path = path
            });
            fired++;
        }
        if (fired > 0) await FlagConvertedAsync(sessionId, visitorId);
        await _db.SaveChangesAsync();
    }

    private async Task FlagConvertedAsync(string? sessionId, string? visitorId)
    {
        // H-1: goal fire = canonical "session converted" signal. Flip the flags
        // so Sessions list + Cohorts + Overview report real conversion counts.
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session is not null && !session.Converted) session.Converted = true;
        }
        if (!string.IsNullOrWhiteSpace(visitorId))
        {
            var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == visitorId);
            if (visitor is not null && visitor.ConvertedAt is null)
                visitor.ConvertedAt = DateTime.UtcNow;
        }
    }
}
