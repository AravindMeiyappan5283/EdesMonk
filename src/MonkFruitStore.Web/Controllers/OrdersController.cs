using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonkFruitStore.Application;
using MonkFruitStore.Web.Models;

namespace MonkFruitStore.Web.Controllers;

public sealed class OrdersController : Controller
{
    private readonly IAdminService _admin;
    private readonly ICustomerService _customers;

    public OrdersController(IAdminService admin, ICustomerService customers)
    {
        _admin = admin;
        _customers = customers;
    }

    [Route("/orders/confirmation/{id}")]
    public async Task<IActionResult> Confirmation(string id, CancellationToken ct)
    {
        var order = await _admin.GetOrderAsync(id, ct);
        return order is null ? NotFound() : View(order);
    }

    [Route("/account/orders")]
    [Authorize]
    public async Task<IActionResult> AccountOrders(CancellationToken ct)
    {
        return View(await _customers.GetOrdersAsync(User.GetCustomerId()!, ct));
    }
}
