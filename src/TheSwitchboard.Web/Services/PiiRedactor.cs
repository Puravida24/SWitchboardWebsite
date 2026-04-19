using Serilog.Core;
using Serilog.Events;

namespace TheSwitchboard.Web.Services;

/// <summary>
/// Redacts obvious PII from Serilog log events so Seq dashboards don't leak
/// customer emails, phone numbers, or raw IPs. Property names are matched
/// case-insensitively against a small whitelist and their string values are
/// masked before forwarding to sinks.
/// </summary>
public static class PiiRedactor
{
    private static readonly HashSet<string> EmailProps =
        new(StringComparer.OrdinalIgnoreCase) { "Email", "ToEmail", "SubmitterEmail" };

    private static readonly HashSet<string> PhoneProps =
        new(StringComparer.OrdinalIgnoreCase) { "Phone", "PhoneNumber" };

    private static readonly HashSet<string> IpProps =
        new(StringComparer.OrdinalIgnoreCase) { "Ip", "IpAddress", "ClientIp" };

    public static string MaskEmail(string input)
    {
        var at = input.IndexOf('@');
        if (at <= 2) return "***" + input[Math.Max(0, at)..];
        return input[..2] + "***" + input[at..];
    }

    public static string MaskPhone(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var digits = input.Where(char.IsDigit).ToArray();
        if (digits.Length <= 4) return new string('*', digits.Length);
        var last4 = new string(digits[^4..]);
        return $"***{last4}";
    }

    public static string MaskIp(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.Contains(':'))
        {
            // IPv6 — keep first segment
            var firstSegment = input.Split(':')[0];
            return firstSegment + ":***";
        }
        // IPv4 — keep first octet
        var firstOctet = input.Split('.')[0];
        return firstOctet + ".***";
    }

    // T-6: Stack trace redaction. JS stacks can contain querystrings that carry
    // email / phone / user-provided values. Redact the common patterns before
    // persisting to JsError.StackRedacted.
    private static readonly System.Text.RegularExpressions.Regex EmailInStack =
        new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex PhoneInStack =
        new(@"\+?\d[\d\s().-]{9,}\d", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex SsnInStack =
        new(@"\b\d{3}-\d{2}-\d{4}\b", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex CcInStack =
        new(@"\b(?:\d[ -]*?){13,16}\b", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string RedactStack(string? stack)
    {
        if (string.IsNullOrEmpty(stack)) return string.Empty;
        stack = EmailInStack.Replace(stack, "<email>");
        stack = SsnInStack.Replace(stack, "<ssn>");
        stack = CcInStack.Replace(stack, "<cc>");
        stack = PhoneInStack.Replace(stack, "<phone>");
        return stack;
    }

    /// <summary>Applies masking rules to a Serilog LogEvent in-place.</summary>
    public static void Redact(LogEvent evt)
    {
        foreach (var kvp in evt.Properties.ToList())
        {
            if (kvp.Value is not ScalarValue sv || sv.Value is not string s) continue;

            string? masked = null;
            if (EmailProps.Contains(kvp.Key)) masked = MaskEmail(s);
            else if (PhoneProps.Contains(kvp.Key)) masked = MaskPhone(s);
            else if (IpProps.Contains(kvp.Key)) masked = MaskIp(s);

            if (masked is not null)
            {
                evt.AddOrUpdateProperty(new LogEventProperty(kvp.Key, new ScalarValue(masked)));
            }
        }
    }
}

/// <summary>Serilog enricher wrapper that invokes <see cref="PiiRedactor.Redact"/>.</summary>
public class PiiRedactionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        PiiRedactor.Redact(logEvent);
    }
}
