using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Shopify;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyAccessTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ShopifyOAuthOptions _shopifyOAuthOptions;
    private readonly IDataProtector _tokenProtector;

    public ShopifyAccessTokenService(
        AppDbContext dbContext,
        HttpClient httpClient,
        IConfiguration configuration,
        IOptions<ShopifyOAuthOptions> shopifyOAuthOptions,
        IDataProtectionProvider dataProtectionProvider)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _configuration = configuration;
        _shopifyOAuthOptions = shopifyOAuthOptions.Value;
        _tokenProtector = dataProtectionProvider.CreateProtector(
            "Shopify.StoreAccessTokens.v1");
    }

    public async Task<string> GetAccessTokenAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);

        if (store == null)
        {
            throw new InvalidOperationException(
                $"Store {storeId} was not found.");
        }

        if (string.IsNullOrWhiteSpace(store.ShopDomain))
        {
            throw new InvalidOperationException(
                $"Store {storeId} has no Shopify domain.");
        }

        // New OAuth-connected stores.
        if (!string.IsNullOrWhiteSpace(store.ShopifyAccessTokenEncrypted) &&
            !string.IsNullOrWhiteSpace(store.ShopifyRefreshTokenEncrypted))
        {
            if (store.ShopifyAccessTokenExpiresAtUtc.HasValue &&
                store.ShopifyAccessTokenExpiresAtUtc.Value >
                DateTime.UtcNow.AddMinutes(5))
            {
                return _tokenProtector.Unprotect(
                    store.ShopifyAccessTokenEncrypted);
            }

            return await RefreshOAuthTokenAsync(
                store,
                cancellationToken);
        }

        // Temporary fallback for existing stores such as Ayzon.
        return await GetLegacyAccessTokenAsync(
            storeId,
            store.ShopDomain,
            cancellationToken);
    }

    private async Task<string> RefreshOAuthTokenAsync(
        Core.Entities.Store store,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(store.ShopifyRefreshTokenEncrypted))
        {
            throw new InvalidOperationException(
                $"Store {store.Id} has no Shopify refresh token.");
        }

        if (store.ShopifyRefreshTokenExpiresAtUtc.HasValue &&
            store.ShopifyRefreshTokenExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                $"Shopify authorization for store {store.Id} has expired. Reconnect the store.");
        }

        var refreshToken = _tokenProtector.Unprotect(
            store.ShopifyRefreshTokenEncrypted);

        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _shopifyOAuthOptions.ClientId,
            ["client_secret"] = _shopifyOAuthOptions.ClientSecret,
            ["refresh_token"] = refreshToken
        };

        using var response = await _httpClient.PostAsync(
            $"https://{store.ShopDomain}/admin/oauth/access_token",
            new FormUrlEncodedContent(request),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(
                cancellationToken);

            store.LastShopifyError =
                $"OAuth token refresh failed: {(int)response.StatusCode}";

            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Shopify token refresh failed for store {store.Id}: {error}");
        }

        var tokenResponse =
            await response.Content.ReadFromJsonAsync<ShopifyRefreshTokenResponse>(
                cancellationToken: cancellationToken);

        if (tokenResponse == null ||
            string.IsNullOrWhiteSpace(tokenResponse.AccessToken) ||
            string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException(
                $"Shopify returned an invalid refresh response for store {store.Id}.");
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

        if (!string.IsNullOrWhiteSpace(tokenResponse.Scope))
        {
            store.ShopifyGrantedScopes = tokenResponse.Scope;
        }

        store.LastShopifyError = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return tokenResponse.AccessToken;
    }

    private async Task<string> GetLegacyAccessTokenAsync(
        int storeId,
        string shopDomain,
        CancellationToken cancellationToken)
    {
        var clientId =
            _configuration[$"Shopify:Stores:{storeId}:ClientId"];

        var clientSecret =
            _configuration[$"Shopify:Stores:{storeId}:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"Store {storeId} is not connected to Shopify.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{shopDomain}/admin/oauth/access_token");

        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to obtain Shopify access token for Store {storeId}. Status {(int)response.StatusCode}.");
        }

        using var json = JsonDocument.Parse(responseText);

        if (!json.RootElement.TryGetProperty(
                "access_token",
                out var accessTokenElement))
        {
            throw new InvalidOperationException(
                "Shopify access-token response did not contain an access token.");
        }

        return accessTokenElement.GetString()
            ?? throw new InvalidOperationException(
                "Shopify returned an empty access token.");
    }

    private sealed class ShopifyRefreshTokenResponse
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