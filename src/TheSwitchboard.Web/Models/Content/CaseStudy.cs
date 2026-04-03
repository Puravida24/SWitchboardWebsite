namespace TheSwitchboard.Web.Models.Content;

public class CaseStudy
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Industry { get; set; }
    public string? ClientName { get; set; }
    public string? Challenge { get; set; }
    public string? Solution { get; set; }
    public string? Results { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? StatLabel1 { get; set; }
    public string? StatValue1 { get; set; }
    public string? StatLabel2 { get; set; }
    public string? StatValue2 { get; set; }
    public string? StatLabel3 { get; set; }
    public string? StatValue3 { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
