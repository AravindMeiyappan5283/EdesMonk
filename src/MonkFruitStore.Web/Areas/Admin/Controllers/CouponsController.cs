using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class CouponsController : Controller
{
    private readonly IAdminService _admin;

    public CouponsController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _admin.GetCouponsAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Coupon coupon, CancellationToken ct)
    {
        try
        {
            await _admin.SaveCouponAsync(coupon, ct);
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
