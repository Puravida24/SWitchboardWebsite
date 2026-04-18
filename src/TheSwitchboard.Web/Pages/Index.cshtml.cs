using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages;

public class IndexModel : PublicPageModel
{
    public IndexModel(IWebHostEnvironment env, AppDbContext db) : base(env, db) { }
    protected override string SourceFile => "wireframes/design-32e-newsprint.html";
}
