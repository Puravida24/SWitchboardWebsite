namespace TheSwitchboard.Web.Models.Content;

/// <summary>
/// Append-only snapshot of a single content-field edit in admin. Slice 3 exposes
/// the last 10 per (EntityType, EntityKey, FieldName) in /Admin/History for revert.
/// </summary>
public class ContentVersion
{
    public int Id { get; set; }

    /// <summary>"sitesettings", "partner", "legalpage".</summary>
    public required string EntityType { get; set; }

    /// <summary>Natural key of the entity — e.g. "1" for SiteSettings singleton or "privacy" for legal page slug.</summary>
    public required string EntityKey { get; set; }

    public required string FieldName { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
