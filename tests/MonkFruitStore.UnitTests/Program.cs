using MonkFruitStore.Domain;

var product = new Product
{
    Name = "Smoke Test Product",
    Sku = "SMOKE-001",
    Slug = "smoke-test-product",
    PackSize = "1 pack",
    Mrp = 100,
    SellingPrice = 90,
    StockQuantity = 5
};

if (product.SellingPrice <= 0 || product.StockQuantity <= 0)
{
    throw new InvalidOperationException("Product smoke test failed.");
}

Console.WriteLine("Smoke tests passed.");
