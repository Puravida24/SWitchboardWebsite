using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// H-1.G — EmailService must support comma-separated recipient lists so
/// Railway env var Email:InternalNotificationAddress can route a single
/// submission to multiple team mailboxes (e.g., 'jason@..,jmarks@..').
/// </summary>
public class EmailRecipientTests
{
    [Fact]
    public void ParseRecipients_CommaSeparated_ReturnsAll()
    {
        var list = EmailService.ParseRecipients("jason@theswitchboardmarketing.com,jmarks@theswitchboardmarketing.com").ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("jason@theswitchboardmarketing.com", list[0]);
        Assert.Equal("jmarks@theswitchboardmarketing.com", list[1]);
    }

    [Fact]
    public void ParseRecipients_SingleAddress_ReturnsOne()
    {
        var list = EmailService.ParseRecipients("only@x.com").ToList();
        Assert.Single(list);
        Assert.Equal("only@x.com", list[0]);
    }

    [Fact]
    public void ParseRecipients_TrimsWhitespaceAndSkipsEmpties()
    {
        var list = EmailService.ParseRecipients("  a@x.com , ,  b@x.com  ").ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("a@x.com", list[0]);
        Assert.Equal("b@x.com", list[1]);
    }

    [Fact]
    public void ParseRecipients_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(EmailService.ParseRecipients(null));
        Assert.Empty(EmailService.ParseRecipients(""));
        Assert.Empty(EmailService.ParseRecipients("   "));
    }

    [Fact]
    public void ParseRecipients_AlsoAcceptsSemicolons()
    {
        // Outlook-style semicolon separation — belt-and-suspenders.
        var list = EmailService.ParseRecipients("a@x.com;b@x.com").ToList();
        Assert.Equal(2, list.Count);
    }
}
