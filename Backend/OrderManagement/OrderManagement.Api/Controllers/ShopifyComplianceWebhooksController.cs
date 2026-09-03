using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Shopify;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/webhooks/shopify/compliance")]
public class ShopifyComplianceWebhooksController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ShopifyOAuthOptions _shopifyOAuthOptions;
    private readonly ILogger<ShopifyComplianceWebhooksController> _logger;

    public ShopifyComplianceWebhooksController(
        AppDbContext dbContext,
        IOptions<ShopifyOAuthOptions> shopifyOAuthOptions,
        ILogger<ShopifyComplianceWebhooksController> logger)
    {
        _dbContext = dbContext;
        _shopifyOAuthOptions = shopifyOAuthOptions.Value;
        _logger = logger;
    }

    [HttpPost("customers-data-request")]
    [AllowAnonymous]
    public async Task<IActionResult> CustomersDataRequest(
        CancellationToken cancellationToken)
    {
        var rawBody = await GetValidatedBodyAsync(cancellationToken);

        if (rawBody is null)
        {
            return Unauthorized();
        }

        using var payload = JsonDocument.Parse(rawBody);

        var shopDomain = GetString(payload.RootElement, "shop_domain");
        var customerId = GetCustomerId(payload.RootElement);

        _logger.LogInformation(
            "Shopify customer data request received. Shop: {ShopDomain}, CustomerId: {CustomerId}",
            shopDomain,
            customerId);

        return Ok();
    }

    [HttpPost("customers-redact")]
    [AllowAnonymous]
    public async Task<IActionResult> CustomersRedact(
        CancellationToken cancellationToken)
    {
        var rawBody = await GetValidatedBodyAsync(cancellationToken);

        if (rawBody is null)
        {
            return Unauthorized();
        }

        using var payload = JsonDocument.Parse(rawBody);

        var shopDomain = GetString(payload.RootElement, "shop_domain");
        var customerId = GetCustomerId(payload.RootElement);

        if (string.IsNullOrWhiteSpace(shopDomain) ||
            !customerId.HasValue)
        {
            return BadRequest();
        }

        var store = await _dbContext.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ShopDomain == shopDomain,
                cancellationToken);

        if (store is null)
        {
            return Ok();
        }

        var externalCustomerId =
            $"gid://shopify/Customer/{customerId.Value}";

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(
                x => x.StoreId == store.Id &&
                     x.ExternalCustomerId == externalCustomerId,
                cancellationToken);

        if (customer is null)
        {
            return Ok();
        }

        customer.ExternalCustomerId = null;
        customer.FullName = "Redacted Customer";
        customer.Phone = string.Empty;
        customer.AddressLine1 = string.Empty;
        customer.City = string.Empty;
        customer.Country = string.Empty;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shopify customer data redacted. StoreId: {StoreId}",
            store.Id);

        return Ok();
    }

    [HttpPost("shop-redact")]
    [AllowAnonymous]
    public async Task<IActionResult> ShopRedact(
        CancellationToken cancellationToken)
    {
        var rawBody = await GetValidatedBodyAsync(cancellationToken);

        if (rawBody is null)
        {
            return Unauthorized();
        }

        using var payload = JsonDocument.Parse(rawBody);

        var shopDomain = GetString(payload.RootElement, "shop_domain");

        if (string.IsNullOrWhiteSpace(shopDomain))
        {
            return BadRequest();
        }

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(
                x => x.ShopDomain == shopDomain,
                cancellationToken);

        if (store is null)
        {
            return Ok();
        }

        var customers = await _dbContext.Customers
            .Where(x => x.StoreId == store.Id)
            .ToListAsync(cancellationToken);

        foreach (var customer in customers)
        {
            customer.ExternalCustomerId = null;
            customer.FullName = "Redacted Customer";
            customer.Phone = string.Empty;
            customer.AddressLine1 = string.Empty;
            customer.City = string.Empty;
            customer.Country = string.Empty;
            customer.UpdatedAtUtc = DateTime.UtcNow;
        }

        store.ShopifyAccessTokenEncrypted = null;
        store.ShopifyRefreshTokenEncrypted = null;
        store.ShopifyAccessTokenExpiresAtUtc = null;
        store.ShopifyRefreshTokenExpiresAtUtc = null;
        store.ShopifyGrantedScopes = null;
        store.ShopifyConnectedAtUtc = null;

        store.ShopifyOAuthStateHash = null;
        store.ShopifyOAuthStateExpiresAtUtc = null;

        store.LastSuccessfulSyncAtUtc = null;
        store.LastReconciliationAtUtc = null;
        store.LastWebhookReceivedAtUtc = null;
        store.LastShopifyError = null;

        store.ShopDomain = null;
        store.IsActive = false;
        store.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shopify shop data redaction completed. StoreId: {StoreId}",
            store.Id);

        return Ok();
    }

    private async Task<string?> GetValidatedBodyAsync(
        CancellationToken cancellationToken)
    {
        var receivedHmac =
            Request.Headers["X-Shopify-Hmac-SHA256"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(receivedHmac) ||
            string.IsNullOrWhiteSpace(_shopifyOAuthOptions.ClientSecret))
        {
            return null;
        }

        using var reader = new StreamReader(Request.Body);

        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(
                _shopifyOAuthOptions.ClientSecret));

        var calculatedHash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(rawBody));

        byte[] receivedHash;

        try
        {
            receivedHash = Convert.FromBase64String(receivedHmac);
        }
        catch (FormatException)
        {
            return null;
        }

        var valid =
            calculatedHash.Length == receivedHash.Length &&
            CryptographicOperations.FixedTimeEquals(
                calculatedHash,
                receivedHash);

        return valid ? rawBody : null;
    }

    private static string? GetString(
        JsonElement root,
        string propertyName)
    {
        return root.TryGetProperty(
            propertyName,
            out var element)
            ? element.GetString()
            : null;
    }

    private static long? GetCustomerId(JsonElement root)
    {
        if (!root.TryGetProperty(
                "customer",
                out var customer))
        {
            return null;
        }

        if (!customer.TryGetProperty(
                "id",
                out var customerId))
        {
            return null;
        }

        return customerId.TryGetInt64(out var id)
            ? id
            : null;
    }
}