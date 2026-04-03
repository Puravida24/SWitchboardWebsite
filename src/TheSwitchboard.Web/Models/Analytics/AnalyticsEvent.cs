namespace TheSwitchboard.Web.Models.Analytics;

public class AnalyticsEvent
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }
    public string? Label { get; set; }
    public string? Value { get; set; }
    public string? Path { get; set; }
    public string? SessionId { get; set; }
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
