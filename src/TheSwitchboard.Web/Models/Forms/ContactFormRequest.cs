using FluentValidation;

namespace TheSwitchboard.Web.Models.Forms;

public class ContactFormRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Title { get; set; }
    public string? CompanySize { get; set; }
    public string? InsuranceLines { get; set; }
    public string? MonthlyVolume { get; set; }
    public string? BiggestChallenge { get; set; }
    public string? Message { get; set; }
    public bool TcpaConsent { get; set; }
}

public class ContactFormRequestValidator : AbstractValidator<ContactFormRequest>
{
    public ContactFormRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.CompanyName)
            .MaximumLength(200);

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .MaximumLength(5000);

        RuleFor(x => x.TcpaConsent)
            .Equal(true).WithMessage("You must agree to the terms to submit this form");
    }
}
