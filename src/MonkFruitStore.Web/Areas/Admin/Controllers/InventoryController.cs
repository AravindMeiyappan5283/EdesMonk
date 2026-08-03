using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class InventoryController : Controller
{
    private readonly IAdminService _admin;

    public InventoryController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Products = await _admin.GetProductsAsync(ct);
        return View(await _admin.GetInventoryAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentRequest request, CancellationToken ct)
    {
        try
        {
            await _admin.AdjustStockAsync(request, ct);
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
