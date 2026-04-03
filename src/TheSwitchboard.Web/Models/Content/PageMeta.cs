namespace TheSwitchboard.Web.Models.Content;

public class PageMeta
{
    public int Id { get; set; }
    public required string PagePath { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? SchemaMarkup { get; set; }
    public bool NoIndex { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
