using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Models.Booking;
using TheSwitchboard.Web.Models.Content;
using TheSwitchboard.Web.Models.Email;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Site;
using TheSwitchboard.Web.Models.Tracking;

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

    // Slice 5 A/B + launch
    public DbSet<Models.Ab.Experiment> Experiments => Set<Models.Ab.Experiment>();
    public DbSet<Models.Ab.Variant> Variants => Set<Models.Ab.Variant>();
    public DbSet<Models.Ab.AbAssignment> AbAssignments => Set<Models.Ab.AbAssignment>();
    public DbSet<Models.Ab.AbConversion> AbConversions => Set<Models.Ab.AbConversion>();
    public DbSet<Redirect> Redirects => Set<Redirect>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    // T-1 Tracker foundation
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Session> Sessions => Set<Session>();

    // T-3 Signals + bot classification
    public DbSet<BrowserSignal> BrowserSignals => Set<BrowserSignal>();
    public DbSet<KnownProxyAsn> KnownProxyAsns => Set<KnownProxyAsn>();

    // T-4 Clickstream
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

    // T-5 Engagement: scroll / mouse / form funnel
    public DbSet<ScrollSample> ScrollSamples => Set<ScrollSample>();
    public DbSet<MouseTrail> MouseTrails => Set<MouseTrail>();
    public DbSet<FormInteraction> FormInteractions => Set<FormInteraction>();

    // T-6 Performance + errors
    public DbSet<WebVitalSample> WebVitalSamples => Set<WebVitalSample>();
    public DbSet<JsError> JsErrors => Set<JsError>();

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
            // T-2 attribution access patterns:
            e.HasIndex(p => new { p.SessionId, p.Timestamp });
            e.HasIndex(p => p.UtmCampaign);
            e.HasIndex(p => p.LandingFlag);
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

        modelBuilder.Entity<Visitor>(e =>
        {
            e.HasIndex(v => v.LastSeen);
            e.HasIndex(v => v.ConvertedAt);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasIndex(s => s.StartedAt);
            e.HasIndex(s => s.VisitorId);
            e.HasIndex(s => s.IsBot);
        });

        modelBuilder.Entity<BrowserSignal>(e =>
        {
            // One signal row per sid — endpoint is idempotent.
            e.HasIndex(b => b.SessionId).IsUnique();
            e.HasIndex(b => b.CanvasFingerprint);
        });

        modelBuilder.Entity<ClickEvent>(e =>
        {
            e.HasIndex(c => new { c.Path, c.Ts });
            e.HasIndex(c => c.SessionId);
            e.HasIndex(c => new { c.SessionId, c.Selector, c.Ts });
            // Filtered indexes — rage and dead clicks are small slices we query often.
            e.HasIndex(c => c.IsRage).HasFilter("\"IsRage\" = true");
            e.HasIndex(c => c.IsDead).HasFilter("\"IsDead\" = true");
        });

        modelBuilder.Entity<ScrollSample>(e =>
        {
            e.HasIndex(s => new { s.Path, s.Depth });
            // Unique per (sid, path, depth) so the 25/50/75/100 milestones dedup
            // on double-send (client retries, beacon+interval overlap, etc.).
            e.HasIndex(s => new { s.SessionId, s.Path, s.Depth }).IsUnique();
        });

        modelBuilder.Entity<MouseTrail>(e =>
        {
            e.HasIndex(m => new { m.Path, m.Ts });
            e.HasIndex(m => m.SessionId);
        });

        modelBuilder.Entity<FormInteraction>(e =>
        {
            e.HasIndex(f => new { f.SessionId, f.FormId });
            e.HasIndex(f => new { f.FormId, f.FieldName });
            e.HasIndex(f => f.Event);
        });

        modelBuilder.Entity<WebVitalSample>(e =>
        {
            e.HasIndex(v => new { v.Path, v.Metric });
            e.HasIndex(v => v.Ts);
        });

        modelBuilder.Entity<JsError>(e =>
        {
            e.HasIndex(j => j.Ts);
            e.HasIndex(j => j.Fingerprint);
            // One row per (sid, fingerprint) — repeated errors in the same session bump Count.
            e.HasIndex(j => new { j.SessionId, j.Fingerprint }).IsUnique();
        });

        modelBuilder.Entity<KnownProxyAsn>(e =>
        {
            e.HasIndex(a => a.Category);
            // Starter seed — expanded by periodic import in future work.
            e.HasData(
                new KnownProxyAsn { Asn = 16509, Name = "Amazon AWS",          Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 14061, Name = "DigitalOcean",        Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 15169, Name = "Google Cloud",        Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 8075,  Name = "Microsoft Azure",     Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 63949, Name = "Linode",              Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 20473, Name = "Vultr",               Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 24940, Name = "Hetzner",             Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 16276, Name = "OVH",                 Category = "datacenter", UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 9009,  Name = "M247 (VPN backbone)", Category = "vpn",        UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 60068, Name = "CDN77 / Datacamp",    Category = "vpn",        UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 212238,Name = "Datacamp (CyberGhost)", Category = "vpn",      UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 202425,Name = "IP Volume (NordVPN)", Category = "vpn",        UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) },
                new KnownProxyAsn { Asn = 42708, Name = "Portlane (ExpressVPN)", Category = "vpn",      UpdatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc) });
        });
    }
}
