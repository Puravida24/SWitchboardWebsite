using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class ExportsModel : PageModel
{
    private readonly IExportService _exports;
    private readonly IRollupRunner _rollup;
    private readonly IRetentionRunner _retention;

    public ExportsModel(IExportService exports, IRollupRunner rollup, IRetentionRunner retention)
    {
        _exports = exports;
        _rollup = rollup;
        _retention = retention;
    }

    public DateTime FromUtc { get; private set; } = DateTime.UtcNow.AddDays(-30).Date;
    public DateTime ToUtc { get; private set; } = DateTime.UtcNow.Date.AddDays(1);
    public DateTime? LastRollupAt => _rollup.LastRunAt;
    public DateTime? LastRetentionAt => _retention.LastRunAt;

    public void OnGet(DateTime? from, DateTime? to)
    {
        if (from is DateTime f) FromUtc = f.ToUniversalTime();
        if (to is DateTime t) ToUtc = t.ToUniversalTime();
    }

    public async Task<IActionResult> OnGetDownloadAsync(string kind, DateTime from, DateTime to)
    {
        FromUtc = from.ToUniversalTime();
        ToUtc = to.ToUniversalTime();

        var ms = new MemoryStream();
        var rows = kind switch
        {
            "sessions"    => await _exports.WriteSessionsCsvAsync(FromUtc, ToUtc, ms),
            "submissions" => await _exports.WriteFormSubmissionsCsvAsync(FromUtc, ToUtc, ms),
            "clicks"      => await _exports.WriteClicksCsvAsync(FromUtc, ToUtc, ms),
            "errors"      => await _exports.WriteJsErrorsCsvAsync(FromUtc, ToUtc, ms),
            "vitals"      => await _exports.WriteVitalsCsvAsync(FromUtc, ToUtc, ms),
            _             => -1
        };
        if (rows < 0) return NotFound();
        ms.Position = 0;
        var fileName = $"{kind}_{from:yyyyMMdd}_to_{to:yyyyMMdd}.csv";
        return File(ms, "text/csv", fileName);
    }

    public async Task<IActionResult> OnPostRollupNowAsync()
    {
        await _rollup.RollupDayAsync(DateTime.UtcNow.Date.AddDays(-1));
        return RedirectToPage();
    }

    // H-4b — manually replay the last 30 days of daily rollups. Useful when
    // the nightly 02:00 UTC RollupService has missed days (container restart,
    // pg outage) and the admin dashboards show a gap.
    public async Task<IActionResult> OnPostBackfill30dAsync()
    {
        var today = DateTime.UtcNow.Date;
        await _rollup.RollupRangeAsync(today.AddDays(-30), today.AddDays(-1));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRetentionNowAsync()
    {
        await _retention.RunAsync();
        return RedirectToPage();
    }
}
