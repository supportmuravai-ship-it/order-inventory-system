using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Infrastructure.Shopify;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/shopify/oauth")]
public class ShopifyOAuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ShopifyOAuthOptions _shopifyOAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _tokenProtector;
    private readonly ShopifyAdminClient _shopifyAdminClient;

    private readonly ShopifyReconciliationService _shopifyReconciliationService;

    public ShopifyOAuthController(
    IOptions<ShopifyOAuthOptions> shopifyOAuthOptions,
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    ShopifyAdminClient shopifyAdminClient,
    ShopifyReconciliationService shopifyReconciliationService)
    {
        _shopifyOAuthOptions = shopifyOAuthOptions.Value;
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _shopifyAdminClient = shopifyAdminClient;
        _tokenProtector = dataProtectionProvider.CreateProtector(
            "Shopify.StoreAccessTokens.v1");
        _shopifyReconciliationService = shopifyReconciliationService;
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? shop,
        [FromQuery] string? state,
        [FromQuery] string? hmac)
    {
        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(shop) ||
            string.IsNullOrWhiteSpace(state) ||
            string.IsNullOrWhiteSpace(hmac))
        {
            return BadRequest("Missing Shopify OAuth parameters.");
        }

        shop = shop.Trim().ToLowerInvariant();

        if (!IsValidShopDomain(shop))
        {
            return BadRequest("Invalid Shopify shop domain.");
        }

        var stateHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(state)))
            .ToLowerInvariant();

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(x =>
                x.ShopDomain == shop &&
                x.ShopifyOAuthStateHash == stateHash);

        if (store == null)
        {
            return BadRequest("Invalid OAuth state or Shopify store.");
        }

        if (!store.ShopifyOAuthStateExpiresAtUtc.HasValue ||
            store.ShopifyOAuthStateExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return BadRequest("OAuth state has expired.");
        }

        if (!ValidateCallbackHmac(hmac))
        {
            return BadRequest("Invalid Shopify OAuth HMAC.");
        }

        // Consume the state before exchanging the one-time authorization code.
        store.ShopifyOAuthStateHash = null;
        store.ShopifyOAuthStateExpiresAtUtc = null;

        await _dbContext.SaveChangesAsync();

        var httpClient = _httpClientFactory.CreateClient();

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _shopifyOAuthOptions.ClientId,
            ["client_secret"] = _shopifyOAuthOptions.ClientSecret,
            ["code"] = code,
            ["expiring"] = "1"
        };

        using var response = await httpClient.PostAsync(
            $"https://{shop}/admin/oauth/access_token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            return StatusCode(
                StatusCodes.Status502BadGateway,
                $"Shopify token exchange failed: {error}");
        }

        var tokenResponse =
            await response.Content.ReadFromJsonAsync<ShopifyTokenResponse>();

        if (tokenResponse == null ||
            string.IsNullOrWhiteSpace(tokenResponse.AccessToken) ||
            string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                "Shopify returned an invalid token response.");
        }

        var now = DateTime.UtcNow;

        store.ShopifyAccessTokenEncrypted =
            _tokenProtector.Protect(tokenResponse.AccessToken);

        store.ShopifyRefreshTokenEncrypted =
            _tokenProtector.Protect(tokenResponse.RefreshToken);

        store.ShopifyAccessTokenExpiresAtUtc =
            now.AddSeconds(tokenResponse.ExpiresIn);

        store.ShopifyRefreshTokenExpiresAtUtc =
            now.AddSeconds(tokenResponse.RefreshTokenExpiresIn);

        store.ShopifyGrantedScopes = tokenResponse.Scope;
        store.ShopifyConnectedAtUtc = now;
        store.LastShopifyError = null;

        await _dbContext.SaveChangesAsync();

        var webhookUrl =
            $"{_shopifyOAuthOptions.AppBaseUrl.TrimEnd('/')}/api/webhooks/shopify/orders";

        try
        {
            await _shopifyAdminClient.RegisterWebhookAsync(
                store.Id,
                shop,
                "ORDERS_CREATE",
                webhookUrl);

            await _shopifyAdminClient.RegisterWebhookAsync(
                store.Id,
                shop,
                "ORDERS_UPDATED",
                webhookUrl);

            var initialSyncFromUtc = DateTime.UnixEpoch;

            await _shopifyReconciliationService.ReconcileStoreAsync(
                store.Id,
                initialSyncFromUtc,
                HttpContext.RequestAborted);

            store.LastSuccessfulSyncAtUtc = DateTime.UtcNow;
            store.LastReconciliationAtUtc = DateTime.UtcNow;
            store.LastShopifyError = null;

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            store.LastShopifyError = ex.Message;
            await _dbContext.SaveChangesAsync();

            return StatusCode(500, new
            {
                message = "Shopify connected, but webhook registration failed."
            });
        }

        return Ok(new
        {
            message = "Shopify connected, webhooks registered, and initial sync completed.",
            storeId = store.Id,
            shop
        });
    }

    private bool ValidateCallbackHmac(string receivedHmac)
    {
        var parameters = Request.Query
            .Where(x => !string.Equals(
                x.Key,
                "hmac",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}");

        var message = string.Join("&", parameters);

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_shopifyOAuthOptions.ClientSecret));

        var calculatedHash = Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(message)))
            .ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculatedHash),
            Encoding.UTF8.GetBytes(receivedHmac.ToLowerInvariant()));
    }

    private static bool IsValidShopDomain(string shop)
    {
        if (!shop.EndsWith(".myshopify.com", StringComparison.Ordinal))
        {
            return false;
        }

        return Uri.TryCreate(
                   $"https://{shop}",
                   UriKind.Absolute,
                   out var uri) &&
               uri.Host == shop &&
               uri.Scheme == Uri.UriSchemeHttps;
    }

    private sealed class ShopifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token_expires_in")]
        public int RefreshTokenExpiresIn { get; set; }
    }
}