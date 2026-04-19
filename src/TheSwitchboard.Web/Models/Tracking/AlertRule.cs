using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

public class AlertRule
{
    public long Id { get; set; }

    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string MetricExpression { get; set; } = string.Empty;
    [Required, MaxLength(10)]  public string Comparison { get; set; } = "gt";
    public double Threshold { get; set; }
    [Required, MaxLength(10)]  public string Window { get; set; } = "1h";
    [MaxLength(20)]  public string? Channel { get; set; }
    [MaxLength(300)] public string? ChannelTarget { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AlertLog
{
    public long Id { get; set; }
    public long RuleId { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public double Value { get; set; }
    public double Threshold { get; set; }
    [MaxLength(20)]  public string Severity { get; set; } = "warn";
    [MaxLength(2000)] public string? Context { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    [MaxLength(120)] public string? AcknowledgedBy { get; set; }
}
