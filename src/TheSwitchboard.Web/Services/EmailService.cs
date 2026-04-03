using MailKit.Net.Smtp;
using MimeKit;

namespace TheSwitchboard.Web.Services;

public interface IEmailService
{
    Task SendContactConfirmationAsync(string toEmail, string toName);
    Task SendInternalNotificationAsync(string formType, string submitterName, string submitterEmail);
    Task SendAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendContactConfirmationAsync(string toEmail, string toName)
    {
        var subject = "Thanks for reaching out — The Switchboard";
        var body = $"""
            <h2>Thanks for contacting us, {System.Net.WebUtility.HtmlEncode(toName)}!</h2>
            <p>We received your message and will get back to you within 1 business day.</p>
            <p>In the meantime, you can <a href="https://theswitchboardmarketing.com/demo">book a demo</a> to see our platform in action.</p>
            <br>
            <p>— The Switchboard Team</p>
            """;

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendInternalNotificationAsync(string formType, string submitterName, string submitterEmail)
    {
        var internalEmail = _config["Email:InternalNotificationAddress"] ?? "team@theswitchboardmarketing.com";
        var subject = $"New {formType} submission from {submitterName}";
        var body = $"""
            <h3>New {System.Net.WebUtility.HtmlEncode(formType)} submission</h3>
            <p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(submitterName)}</p>
            <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(submitterEmail)}</p>
            <p><a href="https://theswitchboardmarketing.com/admin/forms/submissions">View in admin</a></p>
            """;

        await SendAsync(internalEmail, subject, body);
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured. Email to {To} with subject '{Subject}' was not sent.", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:FromName"] ?? "The Switchboard",
            _config["Email:FromAddress"] ?? "noreply@theswitchboardmarketing.com"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            await client.ConnectAsync(smtpHost, port, MailKit.Security.SecureSocketOptions.StartTls);

            var username = _config["Email:SmtpUsername"];
            var password = _config["Email:SmtpPassword"];
            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, password);

            await client.SendAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
