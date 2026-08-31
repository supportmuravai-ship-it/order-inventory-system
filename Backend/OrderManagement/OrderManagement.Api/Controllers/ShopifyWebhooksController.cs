using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/webhooks/shopify")]
public class ShopifyWebhooksController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ShopifyWebhookVerifier _webhookVerifier;
    private readonly ShopifyAdminClient _shopifyAdminClient;
    private readonly ShopifyOrderSyncService _shopifyOrderSyncService;

    public ShopifyWebhooksController(
        AppDbContext dbContext,
        ShopifyWebhookVerifier webhookVerifier,
        ShopifyAdminClient shopifyAdminClient,
        ShopifyOrderSyncService shopifyOrderSyncService)
    {
        _dbContext = dbContext;
        _webhookVerifier = webhookVerifier;
        _shopifyAdminClient = shopifyAdminClient;
        _shopifyOrderSyncService = shopifyOrderSyncService;
    }

    [HttpPost("orders")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveOrderWebhook(CancellationToken cancellationToken)
    {
        var shopDomain = Request.Headers["X-Shopify-Shop-Domain"].FirstOrDefault();
        var receivedHmac = Request.Headers["X-Shopify-Hmac-SHA256"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(shopDomain) || string.IsNullOrWhiteSpace(receivedHmac))
        {
            return Unauthorized();
        }

        var store = await _dbContext.Stores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.ShopDomain == shopDomain, cancellationToken);

        if (store is null)
        {
            return Unauthorized();
        }

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (!_webhookVerifier.IsValid(store.Id, rawBody, receivedHmac))
        {
            return Unauthorized();
        }

        using var payload = JsonDocument.Parse(rawBody);

        string? externalOrderId = null;

        if (payload.RootElement.TryGetProperty("admin_graphql_api_id", out var graphqlIdElement))
        {
            externalOrderId = graphqlIdElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(externalOrderId) &&
            payload.RootElement.TryGetProperty("id", out var numericIdElement))
        {
            externalOrderId = $"gid://shopify/Order/{numericIdElement.GetInt64()}";
        }

        if (string.IsNullOrWhiteSpace(externalOrderId))
        {
            return BadRequest("Shopify webhook did not contain an order ID.");
        }

        var shopifyOrder = await _shopifyAdminClient.GetOrderByIdAsync(store.Id, store.ShopDomain!, externalOrderId, cancellationToken);

        if (shopifyOrder is null)
        {
            return Ok();
        }

        var syncResult = await _shopifyOrderSyncService.SyncAsync(store.Id, [shopifyOrder], cancellationToken);

        return Ok(syncResult);
    }
}