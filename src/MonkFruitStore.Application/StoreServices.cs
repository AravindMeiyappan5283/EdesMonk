using MonkFruitStore.Domain;
using System.Security.Cryptography;
using System.Text;

namespace MonkFruitStore.Application;

public sealed class StorefrontService : IStorefrontService
{
    private readonly IStoreRepository _repository;

    public StorefrontService(IStoreRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<Product?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products.FirstOrDefault(p => p.IsActive && p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Product?> GetProductByIdAsync(string id, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<StoreSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Settings;
    }
}

public sealed class CartService : ICartService
{
    private readonly IStoreRepository _repository;

    public CartService(IStoreRepository repository) => _repository = repository;

    public async Task<CheckoutQuote> BuildQuoteAsync(IReadOnlyList<CartLine> cart, string? couponCode, CancellationToken ct = default)
    {
        if (cart.Count == 0)
        {
            return new CheckoutQuote();
        }

        if (cart.Count > 10)
        {
            throw new BusinessException("Your cart has too many line items.");
        }

        var store = await _repository.ReadAsync(ct);
        var lines = new List<CheckoutLine>();

        foreach (var item in cart)
        {
            if (item.Quantity is < 1 or > 20)
            {
                throw new BusinessException("Quantity must be between 1 and 20.");
            }

            var product = store.Products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null || !product.IsActive)
            {
                throw new BusinessException("A product in your cart is no longer available.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new BusinessException($"Only {product.StockQuantity} pack(s) of {product.Name} are available.");
            }

            lines.Add(new CheckoutLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                PackSize = product.PackSize,
                ImagePath = product.ImagePath,
                Quantity = item.Quantity,
                UnitPrice = product.SellingPrice,
                LineTotal = product.SellingPrice * item.Quantity
            });
        }

        var subtotal = lines.Sum(x => x.LineTotal);
        var discount = CalculateDiscount(store.Coupons, couponCode, subtotal);
        var taxable = subtotal - discount;
        var shipping = taxable >= store.Settings.FreeShippingThreshold ? 0 : store.Settings.FlatShippingCharge;

        return new CheckoutQuote
        {
            Lines = lines,
            Subtotal = subtotal,
            DiscountAmount = discount,
            ShippingCharge = shipping,
            TaxAmount = 0,
            TotalAmount = taxable + shipping,
            CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim().ToUpperInvariant()
        };
    }

    private static decimal CalculateDiscount(IEnumerable<Coupon> coupons, string? couponCode, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return 0;
        }

        var coupon = coupons.FirstOrDefault(c => c.Code.Equals(couponCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (coupon is null || !coupon.IsActive)
        {
            throw new BusinessException("Coupon code is not valid.");
        }

        if (coupon.ExpiresAt is not null && coupon.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new BusinessException("Coupon code has expired.");
        }

        if (subtotal < coupon.MinimumOrderAmount)
        {
            throw new BusinessException($"Coupon requires a minimum order of ₹{coupon.MinimumOrderAmount:N0}.");
        }

        return decimal.Round(subtotal * coupon.DiscountPercent / 100, 2);
    }
}

public sealed class CheckoutService : ICheckoutService
{
    private readonly IStoreRepository _repository;
    private readonly ICartService _cartService;

    public CheckoutService(IStoreRepository repository, ICartService cartService)
    {
        _repository = repository;
        _cartService = cartService;
    }

    public async Task<Order> CreatePendingOrderAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        ValidateAddress(request.ShippingAddress);
        var quote = await _cartService.BuildQuoteAsync(request.Items, request.CouponCode, ct);
        if (quote.Lines.Count == 0)
        {
            throw new BusinessException("Cart is empty.");
        }

        var store = await _repository.ReadAsync(ct);
        var order = new Order
        {
            OrderNumber = $"EM-{DateTime.UtcNow:yyyyMMdd}-{store.Orders.Count + 1:0000}",
            CustomerId = request.CustomerId,
            ShippingAddress = request.ShippingAddress,
            Items = quote.Lines,
            Subtotal = quote.Subtotal,
            DiscountAmount = quote.DiscountAmount,
            ShippingCharge = quote.ShippingCharge,
            TaxAmount = quote.TaxAmount,
            TotalAmount = quote.TotalAmount,
            CouponCode = quote.CouponCode,
            OrderStatus = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.ManualReview,
            Timeline = new List<string> { $"{DateTimeOffset.UtcNow:u} Pending order created. Razorpay integration is disabled." }
        };

        store.Orders.Add(order);
        await _repository.SaveAsync(store, ct);
        return order;
    }

    private static void ValidateAddress(CheckoutAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.Name) ||
            string.IsNullOrWhiteSpace(address.Email) ||
            string.IsNullOrWhiteSpace(address.Phone) ||
            string.IsNullOrWhiteSpace(address.AddressLine1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) ||
            string.IsNullOrWhiteSpace(address.Pincode))
        {
            throw new BusinessException("Please complete all checkout address fields.");
        }

        if (address.Pincode.Length != 6 || !address.Pincode.All(char.IsDigit) || address.Pincode[0] == '0')
        {
            throw new BusinessException("Please enter a valid Indian PIN code.");
        }
    }
}

public sealed class CustomerService : ICustomerService
{
    private readonly IStoreRepository _repository;

    public CustomerService(IStoreRepository repository) => _repository = repository;

    public async Task<CustomerAccount> RegisterAsync(RegisterCustomerRequest request, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var email = NormalizeEmail(request.Email);

        if (store.Customers.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException("An account already exists for this email.");
        }

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var customer = new CustomerAccount
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            PasswordSalt = salt,
            PasswordHash = HashPassword(request.Password, salt),
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        store.Customers.Add(customer);
        await _repository.SaveAsync(store, ct);
        return customer;
    }

    public async Task<CustomerAccount?> ValidateLoginAsync(string email, string password, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var normalized = NormalizeEmail(email);
        var customer = store.Customers.FirstOrDefault(c => c.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (customer is null)
        {
            return null;
        }

        var hash = HashPassword(password, customer.PasswordSalt);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(customer.PasswordHash)))
        {
            return null;
        }

        customer.LastLoginAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(store, ct);
        return customer;
    }

    public async Task<CustomerAccount?> GetCustomerAsync(string customerId, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Customers.FirstOrDefault(c => c.Id == customerId);
    }

    public async Task<CustomerAccount?> FindByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var normalizedPhone = NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var store = await _repository.ReadAsync(ct);
        return store.Customers.FirstOrDefault(c => NormalizePhone(c.Phone) == normalizedPhone);
    }

    public async Task<CustomerAccount> FindOrCreateByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var normalizedPhone = NormalizePhone(phone);
        if (normalizedPhone.Length is < 10 or > 15)
        {
            throw new BusinessException("Please enter a valid mobile number.");
        }

        var store = await _repository.ReadAsync(ct);
        var existing = store.Customers.FirstOrDefault(c => NormalizePhone(c.Phone) == normalizedPhone);
        if (existing is not null)
        {
            existing.LastLoginAt = DateTimeOffset.UtcNow;
            await _repository.SaveAsync(store, ct);
            return existing;
        }

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var customer = new CustomerAccount
        {
            Name = $"Customer {normalizedPhone}",
            Email = BuildPhoneEmail(normalizedPhone, store.Customers),
            Phone = normalizedPhone,
            PasswordSalt = salt,
            PasswordHash = HashPassword(Guid.NewGuid().ToString("N"), salt),
            IsPhoneOnly = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        store.Customers.Add(customer);
        await _repository.SaveAsync(store, ct);
        return customer;
    }

    public async Task<IReadOnlyList<CartLine>> GetCartAsync(string customerId, CancellationToken ct = default)
    {
        var customer = await GetCustomerAsync(customerId, ct);
        return customer?.Cart.ToList() ?? new List<CartLine>();
    }

    public async Task SaveCartAsync(string customerId, IReadOnlyList<CartLine> cart, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var customer = store.Customers.FirstOrDefault(c => c.Id == customerId) ?? throw new BusinessException("Customer not found.");
        customer.Cart = NormalizeCart(cart);
        await _repository.SaveAsync(store, ct);
    }

    public async Task<IReadOnlyList<CartLine>> MergeCartAsync(string customerId, IReadOnlyList<CartLine> guestCart, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var customer = store.Customers.FirstOrDefault(c => c.Id == customerId) ?? throw new BusinessException("Customer not found.");
        var merged = customer.Cart.Concat(guestCart).ToList();
        customer.Cart = NormalizeCart(merged);
        await _repository.SaveAsync(store, ct);
        return customer.Cart.ToList();
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetSavedAddressesAsync(string customerId, CancellationToken ct = default)
    {
        var customer = await GetCustomerAsync(customerId, ct);
        return customer?.SavedAddresses.OrderByDescending(a => a.CreatedAt).ToList() ?? new List<CustomerAddress>();
    }

    public async Task SaveAddressAsync(string customerId, CheckoutAddress address, string label, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var customer = store.Customers.FirstOrDefault(c => c.Id == customerId) ?? throw new BusinessException("Customer not found.");

        var exists = customer.SavedAddresses.Any(saved =>
            saved.Address.AddressLine1.Equals(address.AddressLine1, StringComparison.OrdinalIgnoreCase) &&
            saved.Address.Pincode == address.Pincode &&
            saved.Address.Phone == address.Phone);

        if (!exists)
        {
            customer.SavedAddresses.Add(new CustomerAddress
            {
                Label = string.IsNullOrWhiteSpace(label) ? "Saved address" : label.Trim(),
                Address = address
            });
        }

        await _repository.SaveAsync(store, ct);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(string customerId, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizePhone(string phone)
    {
        return new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    private static string BuildPhoneEmail(string normalizedPhone, IReadOnlyList<CustomerAccount> customers)
    {
        var email = $"{normalizedPhone}@phone.local";
        if (!customers.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return email;
        }

        return $"{normalizedPhone}-{Guid.NewGuid():N}@phone.local";
    }

    private static List<CartLine> NormalizeCart(IEnumerable<CartLine> cart)
    {
        return cart
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId) && x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new CartLine { ProductId = g.Key, Quantity = Math.Min(20, g.Sum(x => x.Quantity)) })
            .ToList();
    }

    private static string HashPassword(string password, string salt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class AdminService : IAdminService
{
    private readonly IStoreRepository _repository;

    public AdminService(IStoreRepository repository) => _repository = repository;

    public async Task<AdminDashboard> GetDashboardAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var today = DateTimeOffset.UtcNow.Date;
        var lowStock = store.Products.Where(p => p.StockQuantity <= p.LowStockThreshold).OrderBy(p => p.StockQuantity).ToList();

        return new AdminDashboard
        {
            TodaySales = store.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Captured && o.CreatedAt.UtcDateTime.Date == today)
                .Sum(o => o.TotalAmount),
            TotalOrders = store.Orders.Count,
            PendingOrders = store.Orders.Count(o => o.OrderStatus is OrderStatus.PendingPayment or OrderStatus.Processing),
            LowStockProducts = lowStock.Count,
            ActiveProducts = store.Products.Count(p => p.IsActive),
            RecentOrders = store.Orders.OrderByDescending(o => o.CreatedAt).Take(6).ToList(),
            LowStock = lowStock.Take(6).ToList()
        };
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Orders.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<Order?> GetOrderAsync(string id, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Orders.FirstOrDefault(o => o.Id == id);
    }

    public async Task UpdateOrderAsync(OrderUpdateRequest request, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var order = store.Orders.FirstOrDefault(o => o.Id == request.OrderId) ?? throw new BusinessException("Order not found.");
        var wasCaptured = order.PaymentStatus == PaymentStatus.Captured;

        order.OrderStatus = request.OrderStatus;
        order.PaymentStatus = request.PaymentStatus;
        order.TrackingNumber = request.TrackingNumber;
        order.AdminNote = request.AdminNote;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Timeline.Add($"{DateTimeOffset.UtcNow:u} Admin updated order to {order.OrderStatus} / {order.PaymentStatus}.");

        if (!wasCaptured && order.PaymentStatus == PaymentStatus.Captured)
        {
            CaptureInventory(store, order);
            if (order.OrderStatus == OrderStatus.PendingPayment)
            {
                order.OrderStatus = OrderStatus.Confirmed;
            }
        }

        await _repository.SaveAsync(store, ct);
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products.OrderBy(p => p.Name).ToList();
    }

    public async Task<Product?> GetProductAsync(string id, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Products.FirstOrDefault(p => p.Id == id);
    }

    public async Task SaveProductAsync(Product product, CancellationToken ct = default)
    {
        ValidateProduct(product);
        var store = await _repository.ReadAsync(ct);

        product.Slug = Slugify(string.IsNullOrWhiteSpace(product.Slug) ? product.Name : product.Slug);
        product.Sku = product.Sku.Trim().ToUpperInvariant();

        if (store.Products.Any(p => p.Id != product.Id && p.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException("SKU must be unique.");
        }

        if (store.Products.Any(p => p.Id != product.Id && p.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException("Slug must be unique.");
        }

        var existing = store.Products.FirstOrDefault(p => p.Id == product.Id);
        if (existing is null)
        {
            product.Id = string.IsNullOrWhiteSpace(product.Id) ? Guid.NewGuid().ToString("N") : product.Id;
            product.CreatedAt = DateTimeOffset.UtcNow;
            store.Products.Add(product);
        }
        else
        {
            product.CreatedAt = existing.CreatedAt;
            var index = store.Products.IndexOf(existing);
            store.Products[index] = product;
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(store, ct);
    }

    public async Task AdjustStockAsync(StockAdjustmentRequest request, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        var product = store.Products.FirstOrDefault(p => p.Id == request.ProductId) ?? throw new BusinessException("Product not found.");
        var before = product.StockQuantity;
        var after = before + request.QuantityChange;
        if (after < 0)
        {
            throw new BusinessException("Stock cannot go below zero.");
        }

        product.StockQuantity = after;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        store.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            QuantityChange = request.QuantityChange,
            StockBefore = before,
            StockAfter = after,
            Reason = request.Reason,
            CreatedBy = request.CreatedBy
        });

        await _repository.SaveAsync(store, ct);
    }

    public async Task<IReadOnlyList<InventoryTransaction>> GetInventoryAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.InventoryTransactions.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task<IReadOnlyList<Coupon>> GetCouponsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Coupons.OrderBy(c => c.Code).ToList();
    }

    public async Task SaveCouponAsync(Coupon coupon, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(coupon.Code))
        {
            throw new BusinessException("Coupon code is required.");
        }

        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        var store = await _repository.ReadAsync(ct);
        var existing = store.Coupons.FirstOrDefault(c => c.Code.Equals(coupon.Code, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            store.Coupons.Add(coupon);
        }
        else
        {
            var index = store.Coupons.IndexOf(existing);
            store.Coupons[index] = coupon;
        }

        await _repository.SaveAsync(store, ct);
    }

    public async Task<StoreSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        return store.Settings;
    }

    public async Task SaveSettingsAsync(StoreSettings settings, CancellationToken ct = default)
    {
        var store = await _repository.ReadAsync(ct);
        store.Settings = settings;
        await _repository.SaveAsync(store, ct);
    }

    private static void CaptureInventory(StoreSnapshot store, Order order)
    {
        foreach (var item in order.Items)
        {
            var product = store.Products.FirstOrDefault(p => p.Id == item.ProductId) ?? throw new BusinessException("Ordered product no longer exists.");
            if (product.StockQuantity < item.Quantity)
            {
                throw new BusinessException($"Insufficient stock for {product.Name}.");
            }

            var before = product.StockQuantity;
            product.StockQuantity -= item.Quantity;
            product.UpdatedAt = DateTimeOffset.UtcNow;
            store.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                OrderId = order.Id,
                QuantityChange = -item.Quantity,
                StockBefore = before,
                StockAfter = product.StockQuantity,
                Reason = $"Captured payment for order {order.OrderNumber}",
                CreatedBy = "admin:manual-payment"
            });
        }
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name) ||
            string.IsNullOrWhiteSpace(product.Sku) ||
            string.IsNullOrWhiteSpace(product.PackSize) ||
            product.SellingPrice <= 0 ||
            product.Mrp <= 0 ||
            product.StockQuantity < 0)
        {
            throw new BusinessException("Product name, SKU, pack size, valid prices and stock are required.");
        }
    }

    private static string Slugify(string text)
    {
        var chars = text.ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
