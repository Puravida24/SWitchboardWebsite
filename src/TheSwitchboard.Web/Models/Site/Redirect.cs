using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Site;

/// <summary>
/// Single-row 301/302 redirect. FromPath is the key (leading-slash, case-insensitive
/// match at request time). Editable from /Admin/Redirects.
/// </summary>
public class Redirect
{
    [Key, MaxLength(400)]
    public required string FromPath { get; set; }

    [Required, MaxLength(400)]
    public required string ToPath { get; set; }

    /// <summary>HTTP status code — 301 (Moved Permanently) by default, 302 for temp.</summary>
    public int StatusCode { get; set; } = 301;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
