using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Goal evaluation. Called from the form / pageview handlers when an event
/// might match a registered goal; writes a GoalConversion row per hit.
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
        }
        await _db.SaveChangesAsync();
    }

    public async Task EvaluatePageViewAsync(string path, string? sessionId, string? visitorId)
    {
        var goals = await _db.Goals.Where(g => g.Enabled && g.Kind == "pageview").ToListAsync();
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
        }
        await _db.SaveChangesAsync();
    }
}
