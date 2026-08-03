namespace MonkFruitStore.Domain;

public enum OrderStatus
{
    PendingPayment,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public enum PaymentStatus
{
    Pending,
    ManualReview,
    Captured,
    Failed,
    Refunded
}

public sealed class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryId { get; set; } = "sweeteners";
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string PackSize { get; set; } = string.Empty;
    public decimal Mrp { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public string ImagePath { get; set; } = "/images/Edesmonk3.png";
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public sealed class CartLine
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class CustomerAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool IsPhoneOnly { get; set; }
    public List<CartLine> Cart { get; set; } = new();
    public List<CustomerAddress> SavedAddresses { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastLoginAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomerAddress
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "Home";
    public CheckoutAddress Address { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CheckoutAddress
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}

public sealed class CheckoutLine
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string PackSize { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class CheckoutQuote
{
    public List<CheckoutLine> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCharge { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
}

public sealed class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public CheckoutAddress ShippingAddress { get; set; } = new();
    public List<CheckoutLine> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCharge { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public OrderStatus OrderStatus { get; set; } = OrderStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? CouponCode { get; set; }
    public string? TrackingNumber { get; set; }
    public string? AdminNote { get; set; }
    public List<string> Timeline { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Coupon
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class InventoryTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public int QuantityChange { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StoreSettings
{
    public string CompanyName { get; set; } = "EdesMonk";
    public string SupportEmail { get; set; } = "hello@monkfruit.com";
    public string SupportPhone { get; set; } = "+91 00000 00000";
    public string WhatsAppNumber { get; set; } = "+91 00000 00000";
    public string ShippingPolicy { get; set; } = "Orders are prepared after payment confirmation and dispatched from the nearest available fulfilment point.";
    public decimal FreeShippingThreshold { get; set; } = 999;
    public decimal FlatShippingCharge { get; set; } = 50;
}
