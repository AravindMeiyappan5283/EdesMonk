using Microsoft.AspNetCore.Mvc;
using MonkFruitStore.Application;
using MonkFruitStore.Web.Models;

namespace MonkFruitStore.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IStorefrontService _storefront;

    public HomeController(IStorefrontService storefront) => _storefront = storefront;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(new HomeViewModel
        {
            FeaturedProducts = await _storefront.GetFeaturedProductsAsync(ct),
            Settings = await _storefront.GetSettingsAsync(ct)
        });
    }

    [Route("/about")]
    public IActionResult About() => View();

    [Route("/contact")]
    public async Task<IActionResult> Contact(CancellationToken ct)
    {
        return View(await _storefront.GetSettingsAsync(ct));
    }

    [Route("/policies")]
    public async Task<IActionResult> Policies(CancellationToken ct)
    {
        return View(await _storefront.GetSettingsAsync(ct));
    }
}
