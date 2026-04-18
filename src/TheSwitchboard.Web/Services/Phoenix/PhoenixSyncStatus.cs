namespace TheSwitchboard.Web.Services.Phoenix;

/// <summary>
/// State of a form submission's dispatch to the Phoenix CRM webhook.
/// Lives on <see cref="TheSwitchboard.Web.Models.Forms.FormSubmission"/>.
/// </summary>
public enum PhoenixSyncStatus
{
    /// <summary>Initial state — webhook hasn't been attempted yet (or attempted and failed, will retry).</summary>
    Pending = 0,
    /// <summary>Webhook returned 2xx.</summary>
    Sent = 1,
    /// <summary>Webhook returned non-2xx / timeout on the latest attempt; will retry while Attempts &lt; 3.</summary>
    Failed = 2,
    /// <summary>3 consecutive failures — abandoned to dead-letter queue for manual review.</summary>
    DeadLettered = 3
}
