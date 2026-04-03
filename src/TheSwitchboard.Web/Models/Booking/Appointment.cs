namespace TheSwitchboard.Web.Models.Booking;

public class Appointment
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Timezone { get; set; }
    public string Status { get; set; } = "confirmed";
    public string? Notes { get; set; }
    public bool ConfirmationSent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
