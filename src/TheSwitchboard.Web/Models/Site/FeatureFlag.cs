using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Site;

public class FeatureFlag
{
    [Key, MaxLength(120)]
    public required string Key { get; set; }

    public bool IsEnabled { get; set; } = true;

    [MaxLength(400)]
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
