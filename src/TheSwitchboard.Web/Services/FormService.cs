using System.Text.Json;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Forms;

namespace TheSwitchboard.Web.Services;

public interface IFormService
{
    Task<FormSubmission> ProcessSubmissionAsync(string formType, Dictionary<string, string> data, string? ipAddress, string? userAgent, string? sourcePage);
    Task<List<FormSubmission>> GetSubmissionsAsync(int page = 1, int pageSize = 20, string? formType = null);
    Task<FormSubmission?> GetSubmissionByIdAsync(int id);
    Task<int> GetSubmissionCountAsync(string? formType = null);
}

public class FormService : IFormService
{
    private readonly AppDbContext _db;
    private readonly IPhoenixCrmService _crmService;
    private readonly IEmailService _emailService;
    private readonly ILogger<FormService> _logger;
    private readonly HtmlSanitizer _sanitizer;

    public FormService(AppDbContext db, IPhoenixCrmService crmService, IEmailService emailService, ILogger<FormService> logger)
    {
        _db = db;
        _crmService = crmService;
        _emailService = emailService;
        _logger = logger;
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
    }

    public async Task<FormSubmission> ProcessSubmissionAsync(
        string formType,
        Dictionary<string, string> data,
        string? ipAddress,
        string? userAgent,
        string? sourcePage)
    {
        // Sanitize all input values
        var sanitizedData = new Dictionary<string, string>();
        foreach (var kvp in data)
        {
            sanitizedData[kvp.Key] = _sanitizer.Sanitize(kvp.Value);
        }

        var submission = new FormSubmission
        {
            FormType = formType,
            Data = JsonSerializer.Serialize(sanitizedData),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SourcePage = sourcePage
        };

        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Form submission saved: {FormType} #{Id}", formType, submission.Id);

        // Send to Phoenix CRM (fire and forget, don't block response)
        _ = Task.Run(async () =>
        {
            try
            {
                var sent = await _crmService.SendFormSubmissionAsync(formType, sanitizedData);
                if (sent)
                {
                    submission.SentToPhoenix = true;
                    submission.PhoenixResponse = "sent";
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phoenix CRM webhook failed for submission #{Id}", submission.Id);
            }
        });

        // Send emails (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                if (sanitizedData.TryGetValue("email", out var email) && sanitizedData.TryGetValue("firstName", out var name))
                {
                    await _emailService.SendContactConfirmationAsync(email, name);
                    await _emailService.SendInternalNotificationAsync(formType, name, email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed for submission #{Id}", submission.Id);
            }
        });

        return submission;
    }

    public async Task<List<FormSubmission>> GetSubmissionsAsync(int page = 1, int pageSize = 20, string? formType = null)
    {
        var query = _db.FormSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(formType))
            query = query.Where(s => s.FormType == formType);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<FormSubmission?> GetSubmissionByIdAsync(int id)
    {
        return await _db.FormSubmissions.FindAsync(id);
    }

    public async Task<int> GetSubmissionCountAsync(string? formType = null)
    {
        var query = _db.FormSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(formType))
            query = query.Where(s => s.FormType == formType);
        return await query.CountAsync();
    }
}
