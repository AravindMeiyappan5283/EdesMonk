using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkFruitStore.Application;
using MonkFruitStore.Web.Models;

namespace MonkFruitStore.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly ICustomerService _customers;

    public AccountController(ICustomerService customers) => _customers = customers;

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = await _customers.ValidateLoginAsync(model.Email, model.Password, ct);
        if (customer is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInCustomerAsync(customer.Id, customer.Name, customer.Email);
        var merged = await _customers.MergeCartAsync(customer.Id, HttpContext.Session.GetCart(), ct);
        HttpContext.Session.SetCart(merged);
        return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/cart" : model.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpGet("/account/register")]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var customer = await _customers.RegisterAsync(new RegisterCustomerRequest
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Password = model.Password
            }, ct);

            await SignInCustomerAsync(customer.Id, customer.Name, customer.Email);
            var merged = await _customers.MergeCartAsync(customer.Id, HttpContext.Session.GetCart(), ct);
            HttpContext.Session.SetCart(merged);
            return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/cart" : model.ReturnUrl);
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("StoreCookie");
        HttpContext.Session.ClearStoreCart();
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInCustomerAsync(string id, string name, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email)
        };

        await HttpContext.SignInAsync("StoreCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "StoreCookie")));
    }
}
