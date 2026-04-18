using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages;

public class PrivacyModel : PublicPageModel
{
    public PrivacyModel(IWebHostEnvironment env, AppDbContext db) : base(env, db) { }
    protected override string? LegalSlug => "privacy";
}
