using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages;

public class AccessibilityModel : PublicPageModel
{
    public AccessibilityModel(IWebHostEnvironment env, AppDbContext db) : base(env, db) { }
    protected override string SourceFile => "wireframes/accessibility.html";
}
