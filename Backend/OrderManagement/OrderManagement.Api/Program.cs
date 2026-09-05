using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Data.Seed;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Infrastructure.Shopify;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IStoreAccessService, StoreAccessService>();
builder.Services.AddScoped<ICsvOrderImportService,CsvOrderImportService>();

builder.Services.AddHttpClient<ShopifyAdminClient>();
builder.Services.AddHttpClient<ShopifyAccessTokenService>();
builder.Services.AddScoped<ShopifyOrderSyncService>();
builder.Services.AddScoped<ShopifyWebhookVerifier>();
builder.Services.AddScoped<ShopifyReconciliationService>();
builder.Services.AddHostedService<ShopifyReconciliationBackgroundService>();

builder.Services.AddDataProtection();

builder.Services.Configure<ShopifyOAuthOptions>(
    builder.Configuration.GetSection(ShopifyOAuthOptions.SectionName));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Keep password rules reasonable and simple.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    });
});


builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "OrderManagement.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SameSite = SameSiteMode.None;

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.LoginPath = null;
    options.AccessDeniedPath = null;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://proud-wave-0427d0400.3.azurestaticapps.net"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler(options =>
{
    options.ExceptionHandler = async context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred."
        });
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await DevelopmentDataSeeder.SeedAsync(
        dbContext,
        userManager,
        roleManager);
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[]
    {
        "Admin",
        "InventoryManager",
        "CustomerSupport",
        "WarehouseStaff"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }
}

app.UseRateLimiter();


app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();