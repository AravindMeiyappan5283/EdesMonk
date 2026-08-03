using Microsoft.AspNetCore.Mvc;
using MonkFruitStore.Application;

namespace MonkFruitStore.Web.Controllers;

public sealed class ShopController : Controller
{
    private readonly IStorefrontService _storefront;

    public ShopController(IStorefrontService storefront) => _storefront = storefront;

    [Route("/shop")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _storefront.GetActiveProductsAsync(ct));
    }

    [Route("/product/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var product = await _storefront.GetProductBySlugAsync(slug, ct);
        return product is null ? NotFound() : View(product);
    }
}
