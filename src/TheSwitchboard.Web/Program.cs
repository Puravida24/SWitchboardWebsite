using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TheSwitchboard.Web.Api;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Middleware;
using TheSwitchboard.Web.Models.Site;
using TheSwitchboard.Web.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Ensure Log closes once at actual process exit — not on HostAbortedException
// bubbling out of WebApplicationFactory<Program> during integration tests.
AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Railway injects $PORT at runtime. Honor it so we don't need a shell wrapper
    // in the Dockerfile ENTRYPOINT. Falls through to ASPNETCORE_URLS default (8080).
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(port))
        builder.WebHost.UseUrls($"http://+:{port}");

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            // H-07.4: mask Email / Phone / IpAddress values in every log event.
            .Enrich.With<TheSwitchboard.Web.Services.PiiRedactionEnricher>()
            .WriteTo.Console();

        var seqUrl = context.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrEmpty(seqUrl))
            configuration.WriteTo.Seq(seqUrl);
    });

    // Database (optional — uses InMemory when DATABASE_URL or PG is not available)
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("DATABASE_PRIVATE_URL")
        ?? (builder.Environment.IsDevelopment() ? builder.Configuration.GetConnectionString("DefaultConnection") : null);

    // Railway (and Heroku/Render) ship DATABASE_URL as a URI: postgresql://user:pass@host:port/db
    // Npgsql's keyword parser rejects that — translate to Host=...;Port=...;Database=... form.
    if (!string.IsNullOrEmpty(connectionString) &&
        (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
         connectionString.StartsWith("postgres://",   StringComparison.OrdinalIgnoreCase)))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var db = uri.AbsolutePath.TrimStart('/');
        connectionString =
            $"Host={uri.Host};Port={(uri.Port <= 0 ? 5432 : uri.Port)};Database={db};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
    }

    var hasDatabase = !string.IsNullOrEmpty(connectionString);

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        if (hasDatabase)
            options.UseNpgsql(connectionString);
        else
            options.UseInMemoryDatabase(builder.Configuration["Database:InMemoryName"] ?? "SwitchboardDesignReview");

        // HasData seeds reference DateTime.UtcNow via entity defaults → EF sees a
        // "changing model" each boot and would throw PendingModelChangesWarning as
        // an error, failing MigrateAsync(). Ignore it in prod boots.
        options.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });

    // Identity (admin auth)
    builder.Services.AddIdentity<AdminUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireUppercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Replace default PBKDF2 hasher with Argon2id (OWASP 2023 recommended).
    builder.Services.AddSingleton<IPasswordHasher<AdminUser>, Argon2IdPasswordHasher<AdminUser>>();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

    // H-06: explicit HSTS — 1y, subdomains, preload. UseHsts() still gated on
    // non-dev to avoid breaking local HTTP dev loops; options are registered
    // unconditionally so they're observable in tests.
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });

    // Razor Pages
    builder.Services.AddRazorPages()
        .AddRazorPagesOptions(options =>
        {
            options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
            options.Conventions.AllowAnonymousToPage("/Admin/Login");
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));

    // Services
    builder.Services.AddScoped<IContentService, ContentService>();
    builder.Services.AddScoped<IFormService, FormService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IAttributionAnalyticsService,
                               TheSwitchboard.Web.Services.Tracking.AttributionAnalyticsService>();
    builder.Services.AddSingleton<TheSwitchboard.Web.Services.Tracking.IIpClassificationService,
                                  TheSwitchboard.Web.Services.Tracking.IpClassificationService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.ISessionService,
                               TheSwitchboard.Web.Services.Tracking.SessionService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IFrustrationAnalyticsService,
                               TheSwitchboard.Web.Services.Tracking.FrustrationAnalyticsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IEngagementAnalyticsService,
                               TheSwitchboard.Web.Services.Tracking.EngagementAnalyticsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IErrorImpactService,
                               TheSwitchboard.Web.Services.Tracking.ErrorImpactService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IComplianceAnalyticsService,
                               TheSwitchboard.Web.Services.Tracking.ComplianceAnalyticsService>();
    // T-11 Goals / Deploys / DSR.
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IGoalService,
                               TheSwitchboard.Web.Services.Tracking.GoalService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IDsrService,
                               TheSwitchboard.Web.Services.Tracking.DsrService>();

    // T-12 Insights / Alerts / Segments.
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IInsightsService,
                               TheSwitchboard.Web.Services.Tracking.InsightsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IAlertEvaluatorService,
                               TheSwitchboard.Web.Services.Tracking.AlertEvaluatorService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.ISegmentService,
                               TheSwitchboard.Web.Services.Tracking.SegmentService>();
    // H-2 alert delivery + default rules seeder.
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IAlertDispatcher,
                               TheSwitchboard.Web.Services.Tracking.AlertDispatcher>();
    builder.Services.AddHostedService<TheSwitchboard.Web.Services.Tracking.DefaultAlertRulesSeeder>();
    builder.Services.AddHostedService<TheSwitchboard.Web.Services.Tracking.InsightsBackgroundService>();
    builder.Services.AddHostedService<TheSwitchboard.Web.Services.Tracking.AlertEvaluatorBackgroundService>();

    // T-8 Real-time dashboard.
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<TheSwitchboard.Web.Services.Tracking.IRealtimeMetrics,
                                  TheSwitchboard.Web.Services.Tracking.RealtimeMetrics>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IRealtimeBroadcaster,
                               TheSwitchboard.Web.Services.Tracking.RealtimeBroadcaster>();

    // T-10 Rollups / retention / exports.
    builder.Services.AddSingleton<TheSwitchboard.Web.Services.Tracking.IRollupRunner,
                                  TheSwitchboard.Web.Services.Tracking.RollupRunner>();
    builder.Services.AddSingleton<TheSwitchboard.Web.Services.Tracking.IRetentionRunner,
                                  TheSwitchboard.Web.Services.Tracking.RetentionRunner>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IExportService,
                               TheSwitchboard.Web.Services.Tracking.ExportService>();
    builder.Services.AddHostedService<TheSwitchboard.Web.Services.Tracking.RollupBackgroundService>();
    builder.Services.AddHostedService<TheSwitchboard.Web.Services.Tracking.DataRetentionBackgroundService>();

    // T-9 Overview / Trends / Visitors / Cohorts.
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IOverviewService,
                               TheSwitchboard.Web.Services.Tracking.OverviewService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.ITrendsService,
                               TheSwitchboard.Web.Services.Tracking.TrendsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.IVisitorAnalyticsService,
                               TheSwitchboard.Web.Services.Tracking.VisitorAnalyticsService>();
    builder.Services.AddScoped<TheSwitchboard.Web.Services.Tracking.ICohortService,
                               TheSwitchboard.Web.Services.Tracking.CohortService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IImageService, ImageService>();
    builder.Services.AddScoped<IIndexNowService, IndexNowService>();
    builder.Services.AddHttpClient<IPhoenixCrmService, PhoenixCrmService>();

    // H-07.2: rate-limit store. Future: swap to RedisRateLimitStore when
    // builder.Configuration["Redis:ConnectionString"] is populated. For now
    // single-process in-memory bucket.
    builder.Services.AddSingleton<TheSwitchboard.Web.Middleware.IRateLimitStore,
                                   TheSwitchboard.Web.Middleware.InMemoryRateLimitStore>();

    // Validators
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Health checks
    var healthChecks = builder.Services.AddHealthChecks();
    if (hasDatabase)
        healthChecks.AddNpgSql(connectionString!);

    // Response compression — Railway's Fastly edge normally compresses but Lighthouse
    // observed no compression on our HTML. Adding brotli + gzip at the app layer is
    // belt-and-suspenders and handles cases where the edge cache misses.
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "application/xml",
            "application/json",
            "text/plain",
            "image/svg+xml"
        });
    });

    var app = builder.Build();

    app.UseResponseCompression();

    // H-02: honor X-Forwarded-For / X-Forwarded-Proto from the Railway proxy
    // so downstream middleware (rate limit, analytics) see the real client IP
    // rather than the proxy's loopback address.
    var fhOpts = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                           Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    };
    // Railway sits behind a cloud proxy with rotating egress IPs — trust the
    // forwarded headers unconditionally. Safe here because we only honor the
    // two specific headers above, not arbitrary X-Forwarded-*.
    fhOpts.KnownNetworks.Clear();
    fhOpts.KnownProxies.Clear();
    app.UseForwardedHeaders(fhOpts);

    // H-7: attach X-Correlation-ID to every request / log line / response.
    // Runs first so every downstream middleware log entry carries the ID.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Per-request CSP nonce — must run before SecurityHeadersMiddleware so the
    // CSP header can reference the nonce value.
    app.UseMiddleware<CspNonceMiddleware>();

    // Security headers (all environments)
    app.UseMiddleware<SecurityHeadersMiddleware>();
    // H-9f: explicit no-cache on HTML responses so per-request CSP nonces
    // never leak into intermediate caches.
    app.UseMiddleware<HtmlNoCacheMiddleware>();
    app.UseMiddleware<RedirectMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error/500");
        app.UseHsts();
    }

    // Status-code page re-execution applies ONLY to human-facing routes. For /api/*
    // an unhandled non-2xx shouldn't rewrite the status code (which was masking our
    // 403 origin-check response behind a 404 because /Error/403 doesn't exist).
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/api")
            && !ctx.Request.Path.StartsWithSegments("/verify"),
        branch => branch.UseStatusCodePagesWithReExecute("/Error/{0}"));
    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Rate limiting on API endpoints
    app.UseMiddleware<RateLimitMiddleware>();

    // First-party analytics
    app.UseMiddleware<AbTestingMiddleware>();
    app.UseMiddleware<AnalyticsMiddleware>();

    app.UseSerilogRequestLogging();

    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Long cache for /css/*, /js/*, /wireframes/assets/* — file names should be
            // cache-busted via query string when they change. 7 days is safe default.
            var path = ctx.Context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/fonts/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/wireframes/assets/", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800, immutable";
            }
        }
    });
    app.MapRazorPages();
    app.MapHealthChecks("/health");

    // API endpoints
    app.MapContactApi();
    app.MapSeoEndpoints();
    TheSwitchboard.Web.Api.Tracking.TrackingEndpoints.MapTrackingEndpoints(app);
    TheSwitchboard.Web.Api.ConsentMatchEndpoints.MapConsentMatchEndpoints(app);
    app.MapHub<TheSwitchboard.Web.Hubs.RealtimeHub>("/hubs/realtime");
    TheSwitchboard.Web.Api.OpsEndpoints.MapOpsEndpoints(app);
    TheSwitchboard.Web.Api.IndexNowEndpoints.MapIndexNowEndpoints(app);

    // Auto-migrate and seed (each step independent so a dev-only guard failure
    // in one — e.g. AdminSeedService requiring Admin:Password — doesn't cascade).
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (hasDatabase)
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization skipped — running without persistence");
    }

    try { await AdminSeedService.SeedAdminUserAsync(app.Services); }
    catch (Exception ex) { Log.Warning(ex, "Admin user seed skipped"); }

    // MP-1: idempotent seed of marketing partners from Data/Seeds/marketing-partners.txt.
    // Each boot: inserts any file entries not already in the table. Safe to re-run;
    // existing rows (including any admin-managed state) are untouched. Skipped in
    // Testing env so integration tests stay deterministic.
    if (!app.Environment.IsEnvironment("Testing"))
    {
        try
        {
            var mpLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MarketingPartnerSeeder");
            await MarketingPartnerSeeder.SeedMissingAsync(app.Services, app.Environment, mpLogger);
        }
        catch (Exception ex) { Log.Warning(ex, "Marketing partner seed skipped"); }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Log.CloseAndFlush();
}

// Expose Program class for WebApplicationFactory<T> in integration tests.
public partial class Program { }
