using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class ProductsController : Controller
{
    private readonly IAdminService _admin;

    public ProductsController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _admin.GetProductsAsync(ct));
    }

    public async Task<IActionResult> Edit(string? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return View(new Product { IsActive = true, IsFeatured = true, ImagePath = "/images/Edesmonk3.png" });
        }

        var product = await _admin.GetProductAsync(id, ct);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product, CancellationToken ct)
    {
        try
        {
            await _admin.SaveProductAsync(product, ct);
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(product);
        }
    }
}
