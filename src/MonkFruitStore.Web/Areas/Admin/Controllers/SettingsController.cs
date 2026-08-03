using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class SettingsController : Controller
{
    private readonly IAdminService _admin;

    public SettingsController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _admin.GetSettingsAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StoreSettings settings, CancellationToken ct)
    {
        await _admin.SaveSettingsAsync(settings, ct);
        TempData["Saved"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
