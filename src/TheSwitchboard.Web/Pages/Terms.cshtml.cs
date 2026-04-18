using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages;

public class TermsModel : PublicPageModel
{
    public TermsModel(IWebHostEnvironment env, AppDbContext db) : base(env, db) { }
    protected override string SourceFile => "wireframes/terms.html";
}
