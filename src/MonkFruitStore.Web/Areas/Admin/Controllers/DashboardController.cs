using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class DashboardController : Controller
{
    private readonly IAdminService _admin;

    public DashboardController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _admin.GetDashboardAsync(ct));
    }
}
