using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;

namespace MonkFruitStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class OrdersController : Controller
{
    private readonly IAdminService _admin;

    public OrdersController(IAdminService admin) => _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _admin.GetOrdersAsync(ct));
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var order = await _admin.GetOrderAsync(id, ct);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(OrderUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await _admin.UpdateOrderAsync(request, ct);
            return RedirectToAction(nameof(Details), new { id = request.OrderId });
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = request.OrderId });
        }
    }
}
