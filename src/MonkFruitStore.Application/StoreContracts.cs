using System.ComponentModel.DataAnnotations;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Application;

public interface IStoreRepository
{
    Task<StoreSnapshot> ReadAsync(CancellationToken ct = default);
    Task SaveAsync(StoreSnapshot snapshot, CancellationToken ct = default);
}

public interface IStorefrontService
{
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken ct = default);
    Task<Product?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(string id, CancellationToken ct = default);
    Task<StoreSettings> GetSettingsAsync(CancellationToken ct = default);
}

public interface ICartService
{
    Task<CheckoutQuote> BuildQuoteAsync(IReadOnlyList<CartLine> cart, string? couponCode, CancellationToken ct = default);
}

public interface ICheckoutService
{
    Task<Order> CreatePendingOrderAsync(CheckoutRequest request, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<CustomerAccount> RegisterAsync(RegisterCustomerRequest request, CancellationToken ct = default);
    Task<CustomerAccount?> ValidateLoginAsync(string email, string password, CancellationToken ct = default);
    Task<CustomerAccount?> GetCustomerAsync(string customerId, CancellationToken ct = default);
    Task<CustomerAccount?> FindByPhoneAsync(string phone, CancellationToken ct = default);
    Task<CustomerAccount> FindOrCreateByPhoneAsync(string phone, CancellationToken ct = default);
    Task<IReadOnlyList<CartLine>> GetCartAsync(string customerId, CancellationToken ct = default);
    Task SaveCartAsync(string customerId, IReadOnlyList<CartLine> cart, CancellationToken ct = default);
    Task<IReadOnlyList<CartLine>> MergeCartAsync(string customerId, IReadOnlyList<CartLine> guestCart, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerAddress>> GetSavedAddressesAsync(string customerId, CancellationToken ct = default);
    Task SaveAddressAsync(string customerId, CheckoutAddress address, string label, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(string customerId, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<AdminDashboard> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken ct = default);
    Task<Order?> GetOrderAsync(string id, CancellationToken ct = default);
    Task UpdateOrderAsync(OrderUpdateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default);
    Task<Product?> GetProductAsync(string id, CancellationToken ct = default);
    Task SaveProductAsync(Product product, CancellationToken ct = default);
    Task AdjustStockAsync(StockAdjustmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryTransaction>> GetInventoryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Coupon>> GetCouponsAsync(CancellationToken ct = default);
    Task SaveCouponAsync(Coupon coupon, CancellationToken ct = default);
    Task<StoreSettings> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(StoreSettings settings, CancellationToken ct = default);
}

public sealed class StoreSnapshot
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<CustomerAccount> Customers { get; set; } = new();
    public List<Coupon> Coupons { get; set; } = new();
    public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
    public StoreSettings Settings { get; set; } = new();
}

public sealed class CheckoutRequest
{
    [Required]
    public List<CartLine> Items { get; set; } = new();

    [MaxLength(30)]
    public string? CouponCode { get; set; }

    public string? CustomerId { get; set; }

    [Required]
    public CheckoutAddress ShippingAddress { get; set; } = new();
}

public sealed class RegisterCustomerRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed class OrderUpdateRequest
{
    [Required]
    public string OrderId { get; set; } = string.Empty;

    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    [MaxLength(80)]
    public string? TrackingNumber { get; set; }

    [MaxLength(400)]
    public string? AdminNote { get; set; }
}

public sealed class StockAdjustmentRequest
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(-100000, 100000)]
    public int QuantityChange { get; set; }

    [Required, MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = "admin";
}

public sealed class AdminDashboard
{
    public decimal TodaySales { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockProducts { get; set; }
    public int ActiveProducts { get; set; }
    public IReadOnlyList<Order> RecentOrders { get; set; } = Array.Empty<Order>();
    public IReadOnlyList<Product> LowStock { get; set; } = Array.Empty<Product>();
}

public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
