using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Shopify.Models;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyReconciliationService
{
    private readonly AppDbContext _dbContext;
    private readonly ShopifyAdminClient _shopifyAdminClient;
    private readonly ShopifyOrderSyncService _shopifyOrderSyncService;

    public ShopifyReconciliationService(
        AppDbContext dbContext,
        ShopifyAdminClient shopifyAdminClient,
        ShopifyOrderSyncService shopifyOrderSyncService)
    {
        _dbContext = dbContext;
        _shopifyAdminClient = shopifyAdminClient;
        _shopifyOrderSyncService = shopifyOrderSyncService;
    }

    public async Task<ShopifySyncResult> ReconcileStoreAsync(
        int storeId,
        DateTime updatedSinceUtc,
        CancellationToken cancellationToken = default)
    {
        var store = await _dbContext.Stores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == storeId && x.IsActive, cancellationToken);

        if (store is null)
        {
            throw new InvalidOperationException("Store not found.");
        }

        if (string.IsNullOrWhiteSpace(store.ShopDomain))
        {
            throw new InvalidOperationException("Shopify domain is not configured for this store.");
        }

        var finalResult = new ShopifySyncResult();

        string? cursor = null;

        do
        {
            var page = await _shopifyAdminClient.GetUpdatedOrdersPageAsync(
                store.Id,
                store.ShopDomain,
                updatedSinceUtc,
                50,
                cursor,
                cancellationToken);

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

        return finalResult;
    }
}