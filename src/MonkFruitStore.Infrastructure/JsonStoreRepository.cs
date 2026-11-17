using System.Text.Json;
using System.Text.Json.Serialization;
using MonkFruitStore.Application;
using MonkFruitStore.Domain;

namespace MonkFruitStore.Infrastructure;

public sealed class JsonStoreRepository : IStoreRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonStoreRepository(string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "store.json");
    }

    public async Task<StoreSnapshot> ReadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path))
            {
                var seed = CreateSeed();
                await SaveUnsafeAsync(seed, ct);
                return seed;
            }

            var json = await File.ReadAllTextAsync(_path, ct);
            var snapshot = JsonSerializer.Deserialize<StoreSnapshot>(StoreSnapshotText.Clean(json), JsonOptions);
            return snapshot ?? CreateSeed();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(StoreSnapshot snapshot, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SaveUnsafeAsync(snapshot, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveUnsafeAsync(StoreSnapshot snapshot, CancellationToken ct)
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, ct);
    }

    internal static async Task<StoreSnapshot?> TryReadSnapshotFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<StoreSnapshot>(StoreSnapshotText.Clean(json), JsonOptions);
    }

    internal static StoreSnapshot CreateSeed()
    {
        var classicId = Guid.NewGuid().ToString("N");
        var familyId = Guid.NewGuid().ToString("N");
        var travelId = Guid.NewGuid().ToString("N");

        return new StoreSnapshot
        {
            Categories = new List<Category>
            {
                new() { Id = "sweeteners", Name = "Sweeteners", Slug = "sweeteners", DisplayOrder = 1 }
            },
            Products = new List<Product>
            {
                new()
                {
                    Id = classicId,
                    Name = "EdesMonk Classic Blend",
                    Slug = "edesmonk-classic-blend",
                    Sku = "EM-CLASSIC-050",
                    CategoryId = "sweeteners",
                    ShortDescription = "MonkEryth",
                    Description = "A balanced 1:1 sweetness guide for daily drinks, breakfast bowls and simple baking.",
                    Ingredients = "Monk fruit extract blend. Final approved label text should replace this placeholder before launch.",
                    PackSize = "50 servings",
                    Mrp = 349,
                    SellingPrice = 299,
                    StockQuantity = 80,
                    LowStockThreshold = 12,
                    ImagePath = "/images/Edesmonk3.png",
                    IsActive = true,
                    IsFeatured = true
                },
                new()
                {
                    Id = familyId,
                    Name = "EdesMonk Family Pack",
                    Slug = "edesmonk-family-pack",
                    Sku = "EM-FAMILY-150",
                    CategoryId = "sweeteners",
                    ShortDescription = "A larger pack for busy kitchens and repeat use.",
                    Description = "Made for homes that want a dependable sweetener ready for the week.",
                    Ingredients = "Monk fruit extract blend. Final approved label text should replace this placeholder before launch.",
                    PackSize = "150 servings",
                    Mrp = 899,
                    SellingPrice = 799,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    ImagePath = "/images/Edesmonk3.png",
                    IsActive = true,
                    IsFeatured = true
                },
                new()
                {
                    Id = travelId,
                    Name = "EdesMonk Travel Pack",
                    Slug = "edesmonk-travel-pack",
                    Sku = "EM-TRAVEL-020",
                    CategoryId = "sweeteners",
                    ShortDescription = "Portable sweetener for office and travel routines.",
                    Description = "Convenient sachet-style usage for coffees, teas and sweetness on the move.",
                    Ingredients = "Monk fruit extract blend. Final approved label text should replace this placeholder before launch.",
                    PackSize = "20 servings",
                    Mrp = 199,
                    SellingPrice = 149,
                    StockQuantity = 100,
                    LowStockThreshold = 15,
                    ImagePath = "/images/Edesmonk3.png",
                    IsActive = true,
                    IsFeatured = false
                }
            },
            Coupons = new List<Coupon>
            {
                new() { Code = "LAUNCH10", DiscountPercent = 10, MinimumOrderAmount = 299, IsActive = true }
            },
            Settings = new StoreSettings()
        };
    }
}
