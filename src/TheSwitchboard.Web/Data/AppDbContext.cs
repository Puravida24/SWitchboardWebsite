using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Models.Booking;
using TheSwitchboard.Web.Models.Content;
using TheSwitchboard.Web.Models.Email;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Data;

public class AppDbContext : IdentityDbContext<AdminUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Content
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<CaseStudy> CaseStudies => Set<CaseStudy>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ClientLogo> ClientLogos => Set<ClientLogo>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<PageMeta> PageMetas => Set<PageMeta>();

    // Forms
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    // Analytics
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    // Email
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    // Booking
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    // Site
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    // Slice 3 CMS
    public DbSet<LegalPage> LegalPages => Set<LegalPage>();
    public DbSet<ContentVersion> ContentVersions => Set<ContentVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BlogPost>(e =>
        {
            e.HasIndex(b => b.Slug).IsUnique();
            e.HasIndex(b => b.PublishedAt);
            e.HasOne(b => b.Author).WithMany(a => a.BlogPosts).HasForeignKey(b => b.AuthorId);
        });

        modelBuilder.Entity<CaseStudy>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<Author>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();
        });

        modelBuilder.Entity<PageMeta>(e =>
        {
            e.HasIndex(p => p.PagePath).IsUnique();
        });

        modelBuilder.Entity<PageView>(e =>
        {
            e.HasIndex(p => p.Timestamp);
            e.HasIndex(p => p.Path);
        });

        modelBuilder.Entity<AnalyticsEvent>(e =>
        {
            e.HasIndex(ev => ev.Timestamp);
            e.HasIndex(ev => ev.Name);
        });

        modelBuilder.Entity<Subscriber>(e =>
        {
            e.HasIndex(s => s.Email).IsUnique();
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasIndex(a => a.ScheduledAt);
            e.HasIndex(a => a.Email);
        });

        modelBuilder.Entity<SiteSettings>(e =>
        {
            e.HasData(new SiteSettings
            {
                Id = 1,
                SiteName = "The Switchboard",
                SiteTagline = "Insurance Intelligence Platform",
                ContactEmail = "hello@theswitchboardmarketing.com",
                PhoneNumber = "",
                Address = ""
            });
        });
    }
}
