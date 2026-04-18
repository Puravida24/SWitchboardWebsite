using System.Collections.Concurrent;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Tests;

public class FakeEmailService : IEmailService
{
    public ConcurrentBag<(string to, string name)> ConfirmationsSent { get; } = new();
    public ConcurrentBag<(string formType, string name, string email)> NotificationsSent { get; } = new();
    public ConcurrentBag<(string to, string subject, string html)> GenericSends { get; } = new();

    public void Reset()
    {
        while (ConfirmationsSent.TryTake(out _)) { }
        while (NotificationsSent.TryTake(out _)) { }
        while (GenericSends.TryTake(out _)) { }
    }

    public Task SendContactConfirmationAsync(string toEmail, string toName)
    {
        ConfirmationsSent.Add((toEmail, toName));
        return Task.CompletedTask;
    }

    public Task SendInternalNotificationAsync(string formType, string submitterName, string submitterEmail)
    {
        NotificationsSent.Add((formType, submitterName, submitterEmail));
        return Task.CompletedTask;
    }

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        GenericSends.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
