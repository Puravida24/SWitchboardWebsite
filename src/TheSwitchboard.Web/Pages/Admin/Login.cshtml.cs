using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Pages.Admin;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<AdminUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<AdminUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fill in all fields.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Email, Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin user {Email} logged in", Email);
            return RedirectToPage("/Admin/Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Admin user {Email} locked out", Email);
            ErrorMessage = "Account locked. Try again in 15 minutes.";
        }
        else
        {
            ErrorMessage = "Invalid email or password.";
        }

        return Page();
    }
}
