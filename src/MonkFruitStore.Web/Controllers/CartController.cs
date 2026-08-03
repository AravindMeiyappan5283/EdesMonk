using Microsoft.AspNetCore.Mvc;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;
using MonkFruitStore.Web.Models;

namespace MonkFruitStore.Web.Controllers;

public sealed class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ICustomerService _customers;

    public CartController(ICartService cartService, ICustomerService customers)
    {
        _cartService = cartService;
        _customers = customers;
    }

    [Route("/cart")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await BuildViewModelAsync(null, ct));
    }

    [HttpPost("/cart/items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddCartItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var cart = await GetCurrentCartAsync(ct);
        var existing = cart.FirstOrDefault(x => x.ProductId == request.ProductId);
        if (existing is null)
        {
            cart.Add(new CartLine { ProductId = request.ProductId, Quantity = request.Quantity });
        }
        else
        {
            existing.Quantity = Math.Min(20, existing.Quantity + request.Quantity);
        }

        await SaveCurrentCartAsync(cart, ct);
        return RedirectToAction("Index");
    }

    [HttpPost("/cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CartQuantityRequest request, CancellationToken ct)
    {
        var cart = await GetCurrentCartAsync(ct);
        var existing = cart.FirstOrDefault(x => x.ProductId == request.ProductId);
        if (existing is not null)
        {
            if (request.Quantity <= 0)
            {
                cart.Remove(existing);
            }
            else
            {
                existing.Quantity = request.Quantity;
            }
        }

        await SaveCurrentCartAsync(cart, ct);
        return RedirectToAction("Index");
    }

    [HttpPost("/cart/coupon")]
    [ValidateAntiForgeryToken]
    public IActionResult Coupon(string? couponCode)
    {
        HttpContext.Session.SetCoupon(couponCode);
        return RedirectToAction("Index");
    }

    [HttpPost("/cart/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await SaveCurrentCartAsync(new List<CartLine>(), ct);
        HttpContext.Session.ClearStoreCart();
        return RedirectToAction("Index");
    }

    private async Task<CartViewModel> BuildViewModelAsync(string? message, CancellationToken ct)
    {
        try
        {
            return new CartViewModel
            {
                Quote = await _cartService.BuildQuoteAsync(await GetCurrentCartAsync(ct), HttpContext.Session.GetCoupon(), ct),
                CouponCode = HttpContext.Session.GetCoupon(),
                Message = message
            };
        }
        catch (BusinessException ex)
        {
            return new CartViewModel { Message = ex.Message, CouponCode = HttpContext.Session.GetCoupon() };
        }
    }

    private async Task<List<CartLine>> GetCurrentCartAsync(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return HttpContext.Session.GetCart();
        }

        var cart = (await _customers.GetCartAsync(customerId, ct)).ToList();
        HttpContext.Session.SetCart(cart);
        return cart;
    }

    private async Task SaveCurrentCartAsync(IReadOnlyList<CartLine> cart, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            HttpContext.Session.SetCart(cart);
            return;
        }

        await _customers.SaveCartAsync(customerId, cart, ct);
        HttpContext.Session.SetCart(cart);
    }
}
