using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Ab;

namespace TheSwitchboard.Web.Pages.Admin.Experiments;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailModel(AppDbContext db) { _db = db; }

    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    public Experiment? Experiment { get; private set; }
    public List<Row> Rows { get; private set; } = new();

    public record Row(string VariantName, bool IsControl, int Assignments, int Conversions, double Rate);

    public async Task OnGetAsync()
    {
        Experiment = await _db.Experiments.FindAsync(Id);
        if (Experiment is null) return;

        var variants = await _db.Variants.Where(v => v.ExperimentId == Id).ToListAsync();
        var assignments = await _db.AbAssignments.Where(a => a.ExperimentId == Id).ToListAsync();
        var conversions = await _db.AbConversions.Where(c => c.ExperimentId == Id).ToListAsync();

        foreach (var v in variants)
        {
            var aCount = assignments.Count(a => a.VariantId == v.Id);
            var cCount = conversions.Count(c => c.VariantId == v.Id);
            var rate = aCount == 0 ? 0.0 : (double)cCount / aCount;
            Rows.Add(new Row(v.Name, v.IsControl, aCount, cCount, rate));
        }
    }
}
