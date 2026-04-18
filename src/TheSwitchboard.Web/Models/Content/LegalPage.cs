using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Content;

/// <summary>
/// DB-backed legal page content (Privacy / Terms / Accessibility). Slug is the
/// primary key — matches the URL path segment and is used by PublicPageModel
/// subclasses to look up content on each request.
/// </summary>
public class LegalPage
{
    [Key, MaxLength(32)]
    public required string Slug { get; set; }

    public required string HtmlContent { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
