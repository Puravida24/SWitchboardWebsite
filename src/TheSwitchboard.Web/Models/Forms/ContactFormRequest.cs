using System.Text.RegularExpressions;
using FluentValidation;

namespace TheSwitchboard.Web.Models.Forms;

/// <summary>
/// Payload shape posted by the public contact form on /.
/// Matches the &lt;input&gt; names rendered in design-32e-newsprint.html.
/// </summary>
public class ContactFormRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Company { get; set; } = "";
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public string? Message { get; set; }

    /// <summary>
    /// Honeypot. CSS-hidden from humans (<c>input[name=website]{display:none}</c>).
    /// If anything lands in this field we treat the submission as bot traffic and silently
    /// drop it (return 200, do NOT persist, do NOT trigger webhooks or email).
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Client-sent timestamp of when the form page loaded. Used for anti-bot timing:
    /// submissions received &lt; 2s after load are silently dropped (no human fills this fast).
    /// If absent, we skip the timing check (graceful for non-JS clients).
    /// </summary>
    public DateTime? LoadedAt { get; set; }
}

public class ContactFormRequestValidator : AbstractValidator<ContactFormRequest>
{
    // E.164: leading +, 1-3 digit country code, up to 15 total digits. Accepts "+15551234567".
    private static readonly Regex E164 = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);
    private static readonly string[] AllowedRoles = { "carrier", "agency", "mga", "other" };

    public ContactFormRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Company is required")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .Must(p => string.IsNullOrWhiteSpace(p) || E164.IsMatch(p))
            .WithMessage("Phone, if provided, must be in E.164 format (e.g. +15551234567)");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be one of: carrier, agency, mga, other");

        RuleFor(x => x.Message)
            .MaximumLength(5000);
    }
}
