using FluentValidation.TestHelper;
using TheSwitchboard.Web.Models.Forms;

namespace TheSwitchboard.Web.Tests;

public class ContactFormValidationTests
{
    private readonly ContactFormRequestValidator _validator = new();

    private static ContactFormRequest Valid() => new()
    {
        Name = "Sarah Chen",
        Email = "sarah@example.com",
        Company = "Acme Insurance",
        Role = "carrier",
        Message = "Hello"
    };

    [Fact]
    public void Valid_Request_Passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_Name_Fails()
    {
        var r = Valid(); r.Name = "";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Missing_Email_Fails()
    {
        var r = Valid(); r.Email = "";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var r = Valid(); r.Email = "not-an-email";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Missing_Company_Fails()
    {
        var r = Valid(); r.Company = "";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Company);
    }

    [Fact]
    public void Invalid_Role_Fails()
    {
        var r = Valid(); r.Role = "ghost";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Message_Over_5000_Chars_Fails()
    {
        var r = Valid(); r.Message = new string('x', 5001);
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Non_E164_Phone_Fails()
    {
        var r = Valid(); r.Phone = "(555) 123-4567";
        _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void E164_Phone_Passes()
    {
        var r = Valid(); r.Phone = "+15551234567";
        _validator.TestValidate(r).ShouldNotHaveValidationErrorFor(x => x.Phone);
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
