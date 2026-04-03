using TheSwitchboard.Web.Models.Content;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Site;
using TheSwitchboard.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace TheSwitchboard.Web.Tests;

public class ModelTests
{
    [Fact]
    public void BlogPost_RequiredFields_AreSet()
    {
        var post = new BlogPost { Title = "Test Post", Slug = "test-post" };
        Assert.Equal("Test Post", post.Title);
        Assert.Equal("test-post", post.Slug);
        Assert.False(post.IsPublished);
        Assert.True(post.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CaseStudy_RequiredFields_AreSet()
    {
        var study = new CaseStudy { Title = "Case", Slug = "case-slug", Industry = "Auto" };
        Assert.Equal("Auto", study.Industry);
        Assert.False(study.IsPublished);
    }

    [Fact]
    public void Testimonial_DefaultsToActive()
    {
        var t = new Testimonial
        {
            Quote = "Great product",
            PersonName = "Sarah",
            PersonTitle = "VP",
            CompanyName = "Acme"
        };
        Assert.True(t.IsActive);
    }

    [Fact]
    public void FormSubmission_TracksCreatedAt()
    {
        var sub = new FormSubmission { FormType = "contact", Data = "{}" };
        Assert.True(sub.CreatedAt <= DateTime.UtcNow);
        Assert.False(sub.SentToPhoenix);
    }

    [Fact]
    public void PageView_DefaultTimestamp()
    {
        var pv = new PageView { Path = "/about" };
        Assert.True(pv.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void SiteSettings_HasRequiredFields()
    {
        var settings = new SiteSettings { SiteName = "The Switchboard" };
        Assert.Equal("The Switchboard", settings.SiteName);
    }
}

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Middleware_Sets_SecurityHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains("camera=()", context.Response.Headers["Permissions-Policy"].ToString());
        Assert.Contains("default-src 'self'", context.Response.Headers["Content-Security-Policy"].ToString());
    }
}
