using MailKit.Net.Smtp;
using MimeKit;

namespace TheSwitchboard.Web.Services;

public interface IEmailService
{
    Task SendContactConfirmationAsync(string toEmail, string toName);
    Task SendInternalNotificationAsync(string formType, string submitterName, string submitterEmail, IDictionary<string, string>? fields = null);
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
            <br>
            <p>— The Switchboard Team</p>
            """;

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendInternalNotificationAsync(string formType, string submitterName, string submitterEmail, IDictionary<string, string>? fields = null)
    {
        var internalEmail = _config["Email:InternalNotificationAddress"] ?? "team@theswitchboardmarketing.com";
        var subject = $"New {formType} submission from {submitterName}";

        // Render every submitted field (not just name/email). Skip honeypot + timing noise.
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "website", "honeypot", "_honeypot", "loadedat", "_formloadedat" };
        var rows = new System.Text.StringBuilder();
        if (fields is not null)
        {
            foreach (var (key, val) in fields)
            {
                if (string.IsNullOrWhiteSpace(val) || skip.Contains(key)) continue;
                var label = char.ToUpperInvariant(key[0]) + key[1..];
                var encoded = System.Net.WebUtility.HtmlEncode(val).Replace("\n", "<br>");
                rows.AppendLine($"<p><strong>{System.Net.WebUtility.HtmlEncode(label)}:</strong><br>{encoded}</p>");
            }
        }
        if (rows.Length == 0)
        {
            // Fallback (no fields passed) — render at least name + email so the email isn't useless.
            rows.AppendLine($"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(submitterName)}</p>");
            rows.AppendLine($"<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(submitterEmail)}</p>");
        }

        var body = $"""
            <h3>New {System.Net.WebUtility.HtmlEncode(formType)} submission</h3>
            {rows}
            <p><a href="https://www.theswitchboardmarketing.com/Admin/Submissions">View in admin</a></p>
            """;

        await SendAsync(internalEmail, subject, body);
    }

    /// <summary>
    /// H-1.G: split a To-header value (possibly comma- or semicolon-separated)
    /// into individual addresses. Trims whitespace and drops empties.
    /// </summary>
    public static IEnumerable<string> ParseRecipients(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var raw in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = raw.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                yield return trimmed;
        }
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured. Email to {To} with subject '{Subject}' was not sent.", to, subject);
            return;
        }

        var recipients = ParseRecipients(to).ToList();
        if (recipients.Count == 0)
        {
            _logger.LogWarning("Email send aborted — no recipients in '{To}' for subject '{Subject}'.", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:FromName"] ?? "The Switchboard",
            _config["Email:FromAddress"] ?? "noreply@theswitchboardmarketing.com"));
        foreach (var addr in recipients)
            message.To.Add(MailboxAddress.Parse(addr));
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
