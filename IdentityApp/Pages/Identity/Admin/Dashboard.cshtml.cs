using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Pages.Identity.Admin;

public class DashboardModel : AdminPageModel
{
    public DashboardModel(UserManager<IdentityUser> userMgr) => UserManager = userMgr;
    public UserManager<IdentityUser> UserManager { get; set; }

    public int UsersCount { get; set; } = 0;
    public int UsersUnconfirmed { get; set; } = 0;
    public int UsersLockedout { get; set; } = 0;
    public int UsersTwoFactor { get; set; } = 0;

    private readonly string[] emails = { "test@example.com", "test1@example.com", "test2@example.com" };

    public void OnGet()
    {
        UsersCount = UserManager.Users.Count();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        foreach (IdentityUser existingUser in UserManager.Users.ToList())
        {
            IdentityResult result = await UserManager.DeleteAsync(existingUser);
            result.Process(ModelState);
        }

        foreach (string email in emails)
        {
            IdentityUser userObject = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            IdentityResult result = await UserManager.CreateAsync(userObject);
            result.Process(ModelState);
        }
        if (ModelState.IsValid)
        {
            return RedirectToPage();
        }
        return Page();
    }
}
