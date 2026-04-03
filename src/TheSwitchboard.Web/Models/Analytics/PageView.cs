namespace TheSwitchboard.Web.Models.Analytics;

public class PageView
{
    public long Id { get; set; }
    public required string Path { get; set; }
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public string? IpHash { get; set; }
    public string? Country { get; set; }
    public string? SessionId { get; set; }
    public int? ScrollDepthPercent { get; set; }
    public int? TimeOnPageSeconds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
