using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Admin-defined conversion goal. Kind = pageview | event | form | duration.
/// MatchExpression is an opaque string the evaluator service knows how to
/// interpret per Kind (e.g. "contact" for form, "/thank-you" for pageview).
/// </summary>
public class Goal
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>pageview | event | form | duration</summary>
    [Required, MaxLength(20)]
    public string Kind { get; set; } = "form";

    [MaxLength(500)]
    public string? MatchExpression { get; set; }

    public int? Value { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GoalConversion
{
    public long Id { get; set; }
    public long GoalId { get; set; }

    [MaxLength(64)] public string? SessionId { get; set; }
    [MaxLength(64)] public string? VisitorId { get; set; }

    public DateTime Ts { get; set; } = DateTime.UtcNow;
    public int? Value { get; set; }

    [MaxLength(500)]
    public string? Path { get; set; }
}
