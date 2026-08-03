using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AuthController : Controller
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration) => _configuration = configuration;

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var configuredUser = _configuration["Admin:Username"] ?? "admin";
        var configuredPassword = _configuration["Admin:Password"] ?? "EdesMonk@123";

        if (!string.Equals(username, configuredUser, StringComparison.OrdinalIgnoreCase) || password != configuredPassword)
        {
            ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, configuredUser),
            new("role", "admin")
        };

        await HttpContext.SignInAsync("AdminCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookie")));
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToAction(nameof(Login));
    }
}
