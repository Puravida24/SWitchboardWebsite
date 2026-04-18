using System.Text.Json;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Services.Phoenix;

namespace TheSwitchboard.Web.Services;

public interface IFormService
{
    Task<FormSubmission> ProcessContactAsync(ContactFormRequest request, string? ipAddress, string? userAgent, string? sourcePage);
    Task<FormSubmission> ProcessSubmissionAsync(string formType, Dictionary<string, string> data, string? ipAddress, string? userAgent, string? sourcePage);
    Task<List<FormSubmission>> GetSubmissionsAsync(int page = 1, int pageSize = 20, string? formType = null, string? role = null);
    Task<FormSubmission?> GetSubmissionByIdAsync(int id);
    Task<int> GetSubmissionCountAsync(string? formType = null, string? role = null);
    Task MarkEmailBouncedAsync(string email);
    Task<FormSubmission?> RetryPhoenixForSubmissionAsync(int submissionId);
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

    public async Task<FormSubmission> ProcessContactAsync(ContactFormRequest request, string? ipAddress, string? userAgent, string? sourcePage)
    {
        var sanitized = new Dictionary<string, string>
        {
            ["name"] = _sanitizer.Sanitize(request.Name),
            ["email"] = _sanitizer.Sanitize(request.Email),
            ["company"] = _sanitizer.Sanitize(request.Company),
            ["phone"] = _sanitizer.Sanitize(request.Phone ?? ""),
            ["role"] = _sanitizer.Sanitize(request.Role),
            ["message"] = _sanitizer.Sanitize(request.Message ?? ""),
        };

        var submission = new FormSubmission
        {
            FormType = "contact",
            Data = JsonSerializer.Serialize(sanitized),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SourcePage = sourcePage,
            Role = request.Role,
            PhoenixSyncStatus = PhoenixSyncStatus.Pending,
        };
        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Contact submission saved: #{Id}", submission.Id);

        await DispatchPhoenixAsync(submission, sanitized);
        await DispatchEmailsAsync("contact", sanitized);
        return submission;
    }

    // H-4 S2-15: dead-letter after 3 consecutive failures.
    private const int MaxPhoenixAttempts = 3;

    private async Task DispatchPhoenixAsync(FormSubmission submission, Dictionary<string, string> sanitized)
    {
        submission.PhoenixSyncAttempts++;
        submission.LastPhoenixAttemptAt = DateTime.UtcNow;
        try
        {
            var ok = await _crmService.SendFormSubmissionAsync(submission.FormType, sanitized);
            submission.PhoenixSyncStatus = ok
                ? PhoenixSyncStatus.Sent
                : (submission.PhoenixSyncAttempts >= MaxPhoenixAttempts
                    ? PhoenixSyncStatus.DeadLettered
                    : PhoenixSyncStatus.Failed);
            submission.SentToPhoenix = ok;
            submission.PhoenixResponse = ok ? "sent" : "non-2xx";
        }
        catch (Exception ex)
        {
            submission.PhoenixSyncStatus = submission.PhoenixSyncAttempts >= MaxPhoenixAttempts
                ? PhoenixSyncStatus.DeadLettered
                : PhoenixSyncStatus.Failed;
            submission.PhoenixResponse = ex.Message;
            _logger.LogWarning(ex, "Phoenix webhook failed for submission #{Id}", submission.Id);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<FormSubmission?> RetryPhoenixForSubmissionAsync(int submissionId)
    {
        var submission = await _db.FormSubmissions.FindAsync(submissionId);
        if (submission is null) return null;
        if (submission.PhoenixSyncStatus is PhoenixSyncStatus.Sent or PhoenixSyncStatus.DeadLettered)
            return submission;

        Dictionary<string, string>? data;
        try { data = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.Data); }
        catch { data = null; }
        data ??= new();

        await DispatchPhoenixAsync(submission, data);
        return submission;
    }

    private async Task DispatchEmailsAsync(string formType, Dictionary<string, string> sanitized)
    {
        try
        {
            var email = sanitized.GetValueOrDefault("email", "");
            var name = sanitized.GetValueOrDefault("name", "");
            if (!string.IsNullOrWhiteSpace(email))
            {
                await _emailService.SendContactConfirmationAsync(email, name);
                await _emailService.SendInternalNotificationAsync(formType, name, email, sanitized);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email dispatch failed for {FormType}", formType);
        }
    }

    public async Task<FormSubmission> ProcessSubmissionAsync(
        string formType,
        Dictionary<string, string> data,
        string? ipAddress,
        string? userAgent,
        string? sourcePage)
    {
        var sanitizedData = new Dictionary<string, string>();
        foreach (var kvp in data)
            sanitizedData[kvp.Key] = _sanitizer.Sanitize(kvp.Value);

        var submission = new FormSubmission
        {
            FormType = formType,
            Data = JsonSerializer.Serialize(sanitizedData),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SourcePage = sourcePage,
            Role = sanitizedData.GetValueOrDefault("role"),
            PhoenixSyncStatus = PhoenixSyncStatus.Pending
        };

        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Form submission saved: {FormType} #{Id}", formType, submission.Id);

        await DispatchPhoenixAsync(submission, sanitizedData);
        await DispatchEmailsAsync(formType, sanitizedData);
        return submission;
    }

    public async Task<List<FormSubmission>> GetSubmissionsAsync(int page = 1, int pageSize = 20, string? formType = null, string? role = null)
    {
        var query = _db.FormSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(formType)) query = query.Where(s => s.FormType == formType);
        if (!string.IsNullOrEmpty(role)) query = query.Where(s => s.Role == role);
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

    public async Task<int> GetSubmissionCountAsync(string? formType = null, string? role = null)
    {
        var query = _db.FormSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(formType)) query = query.Where(s => s.FormType == formType);
        if (!string.IsNullOrEmpty(role)) query = query.Where(s => s.Role == role);
        return await query.CountAsync();
    }

    public async Task MarkEmailBouncedAsync(string email)
    {
        var rows = await _db.FormSubmissions.Where(s => s.Data.Contains(email)).ToListAsync();
        foreach (var r in rows) r.BouncedEmail = true;
        if (rows.Count > 0) await _db.SaveChangesAsync();
    }
}
