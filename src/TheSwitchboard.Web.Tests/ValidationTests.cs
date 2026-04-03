using FluentValidation.TestHelper;
using TheSwitchboard.Web.Models.Forms;

namespace TheSwitchboard.Web.Tests;

public class ContactFormValidationTests
{
    private readonly ContactFormRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = new ContactFormRequest
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "sarah@example.com",
            TcpaConsent = true
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_FirstName_Fails()
    {
        var request = new ContactFormRequest { LastName = "Chen", Email = "a@b.com", TcpaConsent = true };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Missing_Email_Fails()
    {
        var request = new ContactFormRequest { FirstName = "Sarah", LastName = "Chen", TcpaConsent = true };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var request = new ContactFormRequest { FirstName = "Sarah", LastName = "Chen", Email = "not-an-email", TcpaConsent = true };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Missing_TcpaConsent_Fails()
    {
        var request = new ContactFormRequest { FirstName = "Sarah", LastName = "Chen", Email = "a@b.com", TcpaConsent = false };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TcpaConsent);
    }

    [Fact]
    public void Message_Over_5000_Chars_Fails()
    {
        var request = new ContactFormRequest
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "a@b.com",
            TcpaConsent = true,
            Message = new string('x', 5001)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }
}

public class DemoBookingValidationTests
{
    private readonly DemoBookingRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = new DemoBookingRequest
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "sarah@example.com",
            SelectedDate = DateTime.UtcNow.AddDays(1),
            SelectedTime = "10:00 AM"
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Past_Date_Fails()
    {
        var request = new DemoBookingRequest
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "sarah@example.com",
            SelectedDate = DateTime.UtcNow.AddDays(-1),
            SelectedTime = "10:00 AM"
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SelectedDate);
    }

    [Fact]
    public void Missing_Time_Fails()
    {
        var request = new DemoBookingRequest
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "sarah@example.com",
            SelectedDate = DateTime.UtcNow.AddDays(1)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SelectedTime);
    }
}
