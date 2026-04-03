using Microsoft.AspNetCore.Identity;

namespace TheSwitchboard.Web.Models.Site;

public class AdminUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
