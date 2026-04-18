using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Ab;

public class Experiment
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public required string Name { get; set; }

    [Required, StringLength(120)]
    public required string Slug { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Goal event name that counts as a conversion (e.g., "contact_submit").</summary>
    public string? GoalName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Variant
{
    public int Id { get; set; }
    public int ExperimentId { get; set; }

    [Required, StringLength(120)]
    public required string Name { get; set; }

    /// <summary>Relative weight. Sum across an experiment's variants need not be 100 — normalized at runtime.</summary>
    public int TrafficWeight { get; set; } = 50;

    public bool IsControl { get; set; }
}

public class AbAssignment
{
    public long Id { get; set; }

    [Required, StringLength(64)]
    public required string VisitorKey { get; set; }

    public int ExperimentId { get; set; }
    public int VariantId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class AbConversion
{
    public long Id { get; set; }
    public int ExperimentId { get; set; }
    public int VariantId { get; set; }

    [Required, StringLength(64)]
    public required string VisitorKey { get; set; }

    [Required, StringLength(120)]
    public required string Goal { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}
