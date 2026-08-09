using Microsoft.AspNetCore.DataProtection;
using MonkFruitStore.Application;
using MonkFruitStore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")));
builder.Services.AddAuthentication("StoreCookie")
    .AddCookie("StoreCookie", options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.Cookie.Name = ".EdesMonk.Customer";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    })
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/admin/auth/login";
        options.AccessDeniedPath = "/admin/auth/login";
        options.Cookie.Name = ".EdesMonk.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("AdminCookie");
        policy.RequireClaim("role", "admin");
    });
});
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".EdesMonk.Cart";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddSingleton<IStoreRepository>(services =>
{
    var localRepository = new JsonStoreRepository(builder.Environment.ContentRootPath);
    var provider = builder.Configuration["Store:Provider"];
    if (provider?.Equals("Firebase", StringComparison.OrdinalIgnoreCase) == true ||
        provider?.Equals("Firestore", StringComparison.OrdinalIgnoreCase) == true)
    {
        var projectId = builder.Configuration["Firebase:ProjectId"]
            ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
        var credentialsPath = builder.Configuration["Firebase:CredentialsPath"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (!string.IsNullOrWhiteSpace(credentialsPath))
        {
            var resolvedCredentialsPath = Path.IsPathRooted(credentialsPath)
                ? credentialsPath
                : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, credentialsPath));
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", resolvedCredentialsPath);
        }

        var collectionName = builder.Configuration["Firebase:Collection"] ?? "stores";
        var documentId = builder.Configuration["Firebase:Document"] ?? "edesmonk";
        var localSnapshotPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "store.json");
        var firestoreRepository = new FirestoreStoreRepository(projectId ?? string.Empty, collectionName, documentId, localSnapshotPath);
        var useFallback = builder.Configuration.GetValue("Firebase:FallbackToJsonOnError", true);
        if (useFallback)
        {
            return new FallbackStoreRepository(
                firestoreRepository,
                localRepository,
                services.GetRequiredService<ILogger<FallbackStoreRepository>>());
        }

        return firestoreRepository;
    }

    return localRepository;
});
builder.Services.AddScoped<IStorefrontService, StorefrontService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
