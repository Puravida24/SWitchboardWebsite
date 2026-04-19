using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

public interface ISegmentService
{
    Task<long> CreateAsync(string name, string filterJson, string? createdBy);
    Task<Segment?> GetAsync(long id);
    Task<IReadOnlyList<Segment>> ListAsync();
    Task DeleteAsync(long id);
}

public class SegmentService : ISegmentService
{
    private readonly AppDbContext _db;
    public SegmentService(AppDbContext db) { _db = db; }

    public async Task<long> CreateAsync(string name, string filterJson, string? createdBy)
    {
        var seg = new Segment
        {
            Name = name,
            Filter = string.IsNullOrWhiteSpace(filterJson) ? "{}" : filterJson,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.Segments.Add(seg);
        await _db.SaveChangesAsync();
        return seg.Id;
    }

    public async Task<Segment?> GetAsync(long id) => await _db.Segments.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IReadOnlyList<Segment>> ListAsync() =>
        await _db.Segments.OrderByDescending(s => s.CreatedAt).ToListAsync();

    public async Task DeleteAsync(long id)
    {
        var seg = await _db.Segments.FindAsync(id);
        if (seg is not null) { _db.Segments.Remove(seg); await _db.SaveChangesAsync(); }
    }
}
