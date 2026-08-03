using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Web.Models;

public sealed class HomeViewModel
{
    public IReadOnlyList<Product> FeaturedProducts { get; set; } = Array.Empty<Product>();
    public StoreSettings Settings { get; set; } = new();
}

public sealed class CartViewModel
{
    public CheckoutQuote Quote { get; set; } = new();
    public string? CouponCode { get; set; }
    public string? Message { get; set; }
}

public sealed class CheckoutViewModel
{
    public CheckoutQuote Quote { get; set; } = new();
    public string? CouponCode { get; set; }
    public IReadOnlyList<CustomerAddress> SavedAddresses { get; set; } = Array.Empty<CustomerAddress>();
    public bool IsLoggedIn { get; set; }
    public bool RequiresPhoneLookup { get; set; }
    public bool HasCheckoutCustomer { get; set; }
    public string? CheckoutPhone { get; set; }
    public bool WasCreatedFromPhone { get; set; }
    public bool SaveAddress { get; set; }
    public string AddressLabel { get; set; } = "Home";

    [Required, Display(Name = "Full name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    [Required, Display(Name = "Address")]
    public string AddressLine1 { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required, RegularExpression("^[1-9][0-9]{5}$", ErrorMessage = "Enter a valid Indian PIN code.")]
    public string Pincode { get; set; } = string.Empty;
}

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class AddCartItemRequest
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}

public sealed class CartQuantityRequest
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Quantity { get; set; }
}

public static class CartSession
{
    private const string CartKey = "cart-lines";
    private const string CouponKey = "cart-coupon";
    private const string CheckoutCustomerKey = "checkout-customer-id";
    private const string CheckoutPhoneKey = "checkout-phone";

    public static List<CartLine> GetCart(this ISession session)
    {
        var json = session.GetString(CartKey);
        return string.IsNullOrWhiteSpace(json)
            ? new List<CartLine>()
            : JsonSerializer.Deserialize<List<CartLine>>(json) ?? new List<CartLine>();
    }

    public static void SetCart(this ISession session, IReadOnlyList<CartLine> cart)
    {
        session.SetString(CartKey, JsonSerializer.Serialize(cart));
    }

    public static string? GetCoupon(this ISession session) => session.GetString(CouponKey);

    public static void SetCoupon(this ISession session, string? coupon)
    {
        if (string.IsNullOrWhiteSpace(coupon))
        {
            session.Remove(CouponKey);
        }
        else
        {
            session.SetString(CouponKey, coupon.Trim().ToUpperInvariant());
        }
    }

    public static void ClearStoreCart(this ISession session)
    {
        session.Remove(CartKey);
        session.Remove(CouponKey);
    }

    public static string? GetCheckoutCustomerId(this ISession session) => session.GetString(CheckoutCustomerKey);

    public static string? GetCheckoutPhone(this ISession session) => session.GetString(CheckoutPhoneKey);

    public static void SetCheckoutCustomer(this ISession session, string customerId, string phone)
    {
        session.SetString(CheckoutCustomerKey, customerId);
        session.SetString(CheckoutPhoneKey, phone);
    }

    public static void ClearCheckoutCustomer(this ISession session)
    {
        session.Remove(CheckoutCustomerKey);
        session.Remove(CheckoutPhoneKey);
    }
}

public static class UserIdentityExtensions
{
    public static string? GetCustomerId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
