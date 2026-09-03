using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderManagement.Core.DTOs.Stores;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Infrastructure.Shopify;
using OrderManagement.Infrastructure.Shopify.Models;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.WebRequestMethods;
using System.Text;
namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStoreAccessService _storeAccessService;

    private readonly ShopifyAdminClient _shopifyAdminClient;

    private readonly ShopifyOrderSyncService _shopifyOrderSyncService;

    private readonly ShopifyReconciliationService _shopifyReconciliationService;

    private readonly IConfiguration _configuration;

    private readonly ShopifyOAuthOptions _shopifyOAuthOptions;

    public StoresController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStoreAccessService storeAccessService,
    ShopifyAdminClient shopifyAdminClient,
    ShopifyOrderSyncService shopifyOrderSyncService,
    ShopifyReconciliationService shopifyReconciliationService,
    IConfiguration configuration,
    IOptions<ShopifyOAuthOptions> shopifyOAuthOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _storeAccessService = storeAccessService;
        _shopifyAdminClient = shopifyAdminClient;
        _shopifyOrderSyncService = shopifyOrderSyncService;
        _shopifyReconciliationService = shopifyReconciliationService;
        _configuration = configuration;
        _shopifyOAuthOptions = shopifyOAuthOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<List<StoreDto>>> GetStores()
    {
        var userId = _userManager.GetUserId(User); // 'User' comes from ControllerBase. It represents the currently authenticated request user as a ClaimsPrincipal.

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var stores = await _dbContext.UserStoreAccesses
            .Where(x =>
                x.UserId == userId &&
                x.Store.IsActive)
            .OrderBy(x => x.Store.Name)
            .Select(x => new StoreDto
            {
                Id = x.Store.Id,
                Name = x.Store.Name,
                Code = x.Store.Code
            })
            .ToListAsync();

        return Ok(stores);
    }

    [HttpGet("{storeId:int}/access-test")]
    public async Task<IActionResult> AccessTest(int storeId)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(
            userId,
            storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        return Ok(new
        {
            message = "You have access to this store.",
            storeId
        });
    }

    [HttpPost("{storeId:int}/shopify/sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncShopifyOrders(int storeId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var store = await _dbContext.Stores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == storeId && x.IsActive, cancellationToken);

        if (store is null)
        {
            return NotFound("Store not found.");
        }

        if (string.IsNullOrWhiteSpace(store.ShopDomain))
        {
            return BadRequest("Shopify domain is not configured for this store.");
        }

        var finalResult = new ShopifySyncResult();

        string? cursor = null;

        do
        {
            var page = await _shopifyAdminClient.GetOrdersPageAsync(store.Id, store.ShopDomain, 50, cursor, cancellationToken);

            var pageResult = await _shopifyOrderSyncService.SyncAsync(store.Id, page.Orders, cancellationToken);

            finalResult.Fetched += pageResult.Fetched;
            finalResult.Created += pageResult.Created;
            finalResult.Updated += pageResult.Updated;
            finalResult.Skipped += pageResult.Skipped;
            finalResult.Failed += pageResult.Failed;
            finalResult.Errors.AddRange(pageResult.Errors);

            cursor = page.HasNextPage ? page.EndCursor : null;
        }
        while (cursor is not null);

        return Ok(finalResult);
    }


    [HttpPost("{storeId:int}/shopify/reconcile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReconcileShopifyOrders(int storeId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var updatedSinceUtc = DateTime.UtcNow.AddHours(-24);

        var result = await _shopifyReconciliationService.ReconcileStoreAsync(storeId, updatedSinceUtc, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{storeId:int}/shopify/connect")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConnectShopify(int storeId)
    {
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(x => x.Id == storeId);

        if (store == null)
        {
            return NotFound("Store not found.");
        }

        if (string.IsNullOrWhiteSpace(store.ShopDomain))
        {
            return BadRequest("ShopDomain must be set before connecting Shopify.");
        }

        var shopDomain = store.ShopDomain.Trim().ToLowerInvariant();

        if (!shopDomain.EndsWith(".myshopify.com", StringComparison.Ordinal))
        {
            return BadRequest("Invalid Shopify shop domain.");
        }

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            .ToLowerInvariant();

        var stateHash = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(state)))
    .ToLowerInvariant();

        store.ShopifyOAuthStateHash = stateHash;
        store.ShopifyOAuthStateExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);

        await _dbContext.SaveChangesAsync();

        var redirectUri =
            $"{_shopifyOAuthOptions.AppBaseUrl.TrimEnd('/')}/api/shopify/oauth/callback";

        var authorizationUrl =
            $"https://{shopDomain}/admin/oauth/authorize" +
            $"?client_id={Uri.EscapeDataString(_shopifyOAuthOptions.ClientId)}" +
            $"&scope={Uri.EscapeDataString(_shopifyOAuthOptions.Scopes)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Redirect(authorizationUrl);
    }
}