using Microsoft.AspNetCore.Mvc;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;
using MonkFruitStore.Web.Models;

namespace MonkFruitStore.Web.Controllers;

public sealed class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly ICheckoutService _checkoutService;
    private readonly ICustomerService _customers;

    public CheckoutController(ICartService cartService, ICheckoutService checkoutService, ICustomerService customers)
    {
        _cartService = cartService;
        _checkoutService = checkoutService;
        _customers = customers;
    }

    [HttpGet("/checkout")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (TempData["CheckoutError"] is string error)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        ViewData["CheckoutMessage"] = TempData["CheckoutMessage"];

        var model = await BuildModelAsync(new CheckoutViewModel(), prefillAddress: true, ct);
        if (model.Quote.Lines.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        return View(model);
    }

    [HttpPost("/checkout/phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Phone(string checkoutPhone, CancellationToken ct)
    {
        try
        {
            var existing = await _customers.FindByPhoneAsync(checkoutPhone, ct);
            var customer = await _customers.FindOrCreateByPhoneAsync(checkoutPhone, ct);
            HttpContext.Session.SetCheckoutCustomer(customer.Id, customer.Phone);

            var loggedInCustomerId = User.GetCustomerId();
            if (loggedInCustomerId == customer.Id)
            {
                var customerCart = (await _customers.GetCartAsync(customer.Id, ct)).ToList();
                HttpContext.Session.SetCart(customerCart);
            }
            else
            {
                var mergedCart = await _customers.MergeCartAsync(customer.Id, HttpContext.Session.GetCart(), ct);
                HttpContext.Session.SetCart(mergedCart);
            }

            TempData["CheckoutMessage"] = existing is null
                ? "Mobile number added. Please enter the delivery address."
                : "Saved customer found. Delivery addresses are ready to use or change.";
        }
        catch (BusinessException ex)
        {
            TempData["CheckoutError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/checkout/phone/change")]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePhone()
    {
        HttpContext.Session.ClearCheckoutCustomer();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model, CancellationToken ct)
    {
        model = await BuildModelAsync(model, prefillAddress: false, ct);
        var checkoutCustomerId = HttpContext.Session.GetCheckoutCustomerId();
        if (string.IsNullOrWhiteSpace(checkoutCustomerId))
        {
            ModelState.AddModelError(string.Empty, "Please enter your mobile number before creating the order.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var shippingAddress = new CheckoutAddress
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            AddressLine1 = model.AddressLine1,
            City = model.City,
            State = model.State,
            Pincode = model.Pincode
        };

        try
        {
            var order = await _checkoutService.CreatePendingOrderAsync(new CheckoutRequest
            {
                Items = await GetCurrentCartAsync(ct),
                CouponCode = HttpContext.Session.GetCoupon(),
                CustomerId = checkoutCustomerId,
                ShippingAddress = shippingAddress
            }, ct);

            var savedAddresses = await _customers.GetSavedAddressesAsync(checkoutCustomerId!, ct);
            if (model.SaveAddress || savedAddresses.Count == 0)
            {
                await _customers.SaveAddressAsync(checkoutCustomerId!, shippingAddress, model.AddressLabel, ct);
            }

            await ClearCurrentCartAsync(ct);
            HttpContext.Session.ClearCheckoutCustomer();
            return RedirectToAction("Confirmation", "Orders", new { id = order.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<CheckoutViewModel> BuildModelAsync(CheckoutViewModel model, bool prefillAddress, CancellationToken ct)
    {
        model.CouponCode = HttpContext.Session.GetCoupon();
        model.Quote = await _cartService.BuildQuoteAsync(await GetCurrentCartAsync(ct), model.CouponCode, ct);
        var loggedInCustomerId = User.GetCustomerId();
        var checkoutCustomerId = HttpContext.Session.GetCheckoutCustomerId();
        model.IsLoggedIn = !string.IsNullOrWhiteSpace(loggedInCustomerId);
        model.HasCheckoutCustomer = !string.IsNullOrWhiteSpace(checkoutCustomerId);
        model.RequiresPhoneLookup = !model.HasCheckoutCustomer;
        model.CheckoutPhone = HttpContext.Session.GetCheckoutPhone();

        if (model.HasCheckoutCustomer)
        {
            var customer = await _customers.GetCustomerAsync(checkoutCustomerId!, ct);
            model.WasCreatedFromPhone = customer?.IsPhoneOnly ?? false;
            model.SavedAddresses = await _customers.GetSavedAddressesAsync(checkoutCustomerId!, ct);
            model.SaveAddress = model.SaveAddress || model.SavedAddresses.Count == 0;

            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                model.Phone = model.CheckoutPhone ?? customer?.Phone ?? string.Empty;
            }

            if (prefillAddress && ShouldPrefillAddress(model) && model.SavedAddresses.Count > 0)
            {
                ApplyAddress(model, model.SavedAddresses[0].Address);
            }
        }
        else
        {
            model.SavedAddresses = Array.Empty<CustomerAddress>();
        }

        return model;
    }

    private async Task<List<CartLine>> GetCurrentCartAsync(CancellationToken ct)
    {
        var customerId = HttpContext.Session.GetCheckoutCustomerId() ?? User.GetCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return HttpContext.Session.GetCart();
        }

        var cart = (await _customers.GetCartAsync(customerId, ct)).ToList();
        HttpContext.Session.SetCart(cart);
        return cart;
    }

    private async Task ClearCurrentCartAsync(CancellationToken ct)
    {
        var customerId = HttpContext.Session.GetCheckoutCustomerId() ?? User.GetCustomerId();
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            await _customers.SaveCartAsync(customerId, new List<CartLine>(), ct);
        }

        HttpContext.Session.ClearStoreCart();
    }

    private static bool ShouldPrefillAddress(CheckoutViewModel model)
    {
        return string.IsNullOrWhiteSpace(model.Name) &&
               string.IsNullOrWhiteSpace(model.Email) &&
               string.IsNullOrWhiteSpace(model.AddressLine1) &&
               string.IsNullOrWhiteSpace(model.City) &&
               string.IsNullOrWhiteSpace(model.State) &&
               string.IsNullOrWhiteSpace(model.Pincode);
    }

    private static void ApplyAddress(CheckoutViewModel model, CheckoutAddress address)
    {
        model.Name = address.Name;
        model.Email = address.Email;
        model.Phone = address.Phone;
        model.AddressLine1 = address.AddressLine1;
        model.City = address.City;
        model.State = address.State;
        model.Pincode = address.Pincode;
    }
}
