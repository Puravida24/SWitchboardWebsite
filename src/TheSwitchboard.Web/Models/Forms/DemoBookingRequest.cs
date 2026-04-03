using FluentValidation;

namespace TheSwitchboard.Web.Models.Forms;

public class DemoBookingRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public DateTime SelectedDate { get; set; }
    public string SelectedTime { get; set; } = "";
    public string? Timezone { get; set; }
}

public class DemoBookingRequestValidator : AbstractValidator<DemoBookingRequest>
{
    public DemoBookingRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.SelectedDate).GreaterThan(DateTime.UtcNow.Date).WithMessage("Please select a future date");
        RuleFor(x => x.SelectedTime).NotEmpty().WithMessage("Please select a time slot");
    }
}
