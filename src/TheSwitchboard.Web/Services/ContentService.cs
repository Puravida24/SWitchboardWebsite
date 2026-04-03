using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Services;

public interface IContentService
{
    // Blog
    Task<List<BlogPost>> GetPublishedBlogPostsAsync(int page = 1, int pageSize = 10);
    Task<BlogPost?> GetBlogPostBySlugAsync(string slug);
    Task<int> GetBlogPostCountAsync();

    // Case Studies
    Task<List<CaseStudy>> GetPublishedCaseStudiesAsync(int page = 1, int pageSize = 10);
    Task<CaseStudy?> GetCaseStudyBySlugAsync(string slug);
    Task<List<CaseStudy>> GetFeaturedCaseStudiesAsync(int count = 2);

    // Testimonials
    Task<List<Testimonial>> GetActiveTestimonialsAsync();

    // FAQs
    Task<List<Faq>> GetActiveFaqsAsync(string? category = null);

    // Client Logos
    Task<List<ClientLogo>> GetActiveClientLogosAsync();

    // Team
    Task<List<TeamMember>> GetActiveTeamMembersAsync();

    // Authors
    Task<Author?> GetAuthorBySlugAsync(string slug);

    // Page Meta
    Task<PageMeta?> GetPageMetaAsync(string pagePath);
}

public class ContentService : IContentService
{
    private readonly AppDbContext _db;

    public ContentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BlogPost>> GetPublishedBlogPostsAsync(int page = 1, int pageSize = 10)
    {
        return await _db.BlogPosts
            .Include(b => b.Author)
            .Where(b => b.IsPublished && b.PublishedAt <= DateTime.UtcNow)
            .OrderByDescending(b => b.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<BlogPost?> GetBlogPostBySlugAsync(string slug)
    {
        return await _db.BlogPosts
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);
    }

    public async Task<int> GetBlogPostCountAsync()
    {
        return await _db.BlogPosts
            .CountAsync(b => b.IsPublished && b.PublishedAt <= DateTime.UtcNow);
    }

    public async Task<List<CaseStudy>> GetPublishedCaseStudiesAsync(int page = 1, int pageSize = 10)
    {
        return await _db.CaseStudies
            .Where(c => c.IsPublished)
            .OrderByDescending(c => c.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<CaseStudy?> GetCaseStudyBySlugAsync(string slug)
    {
        return await _db.CaseStudies
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsPublished);
    }

    public async Task<List<CaseStudy>> GetFeaturedCaseStudiesAsync(int count = 2)
    {
        return await _db.CaseStudies
            .Where(c => c.IsPublished)
            .OrderByDescending(c => c.PublishedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Testimonial>> GetActiveTestimonialsAsync()
    {
        return await _db.Testimonials
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<List<Faq>> GetActiveFaqsAsync(string? category = null)
    {
        var query = _db.Faqs.Where(f => f.IsActive);
        if (!string.IsNullOrEmpty(category))
            query = query.Where(f => f.Category == category);
        return await query.OrderBy(f => f.SortOrder).ToListAsync();
    }

    public async Task<List<ClientLogo>> GetActiveClientLogosAsync()
    {
        return await _db.ClientLogos
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TeamMember>> GetActiveTeamMembersAsync()
    {
        return await _db.TeamMembers
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();
    }

    public async Task<Author?> GetAuthorBySlugAsync(string slug)
    {
        return await _db.Authors.FirstOrDefaultAsync(a => a.Slug == slug);
    }

    public async Task<PageMeta?> GetPageMetaAsync(string pagePath)
    {
        return await _db.PageMetas.FirstOrDefaultAsync(p => p.PagePath == pagePath);
    }
}
