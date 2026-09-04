using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShopifyReconciliationBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    public ShopifyReconciliationBackgroundService(
     IServiceScopeFactory scopeFactory,
     ILogger<ShopifyReconciliationBackgroundService> logger,
     IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAllStoresAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shopify reconciliation background run failed.");
            }
        }
    }

    private async Task ReconcileAllStoresAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<ShopifyReconciliationService>();

        var stores = await dbContext.Stores
            .Where(x => x.IsActive && x.ShopDomain != null)
            .ToListAsync(cancellationToken);

        stores = stores
                    .Where(IsShopifyConnected)
                    .ToList();

        foreach (var store in stores)
        {
            try
            {
                var now = DateTime.UtcNow;

                var updatedSinceUtc = store.LastReconciliationAtUtc.HasValue
                    ? store.LastReconciliationAtUtc.Value.AddMinutes(-15)
                    : now.AddHours(-24);

                var result = await reconciliationService.ReconcileStoreAsync(store.Id, updatedSinceUtc, cancellationToken);

                store.LastReconciliationAtUtc = now;
                store.LastSuccessfulSyncAtUtc = now;
                store.LastShopifyError = null;
                store.UpdatedAtUtc = now;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Shopify reconciliation completed for Store {StoreId}. Fetched: {Fetched}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}.",
                    store.Id,
                    result.Fetched,
                    result.Created,
                    result.Updated,
                    result.Skipped,
                    result.Failed);
            }
            catch (Exception ex)
            {
                store.LastShopifyError = ex.Message;
                store.UpdatedAtUtc = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogError(ex, "Shopify reconciliation failed for Store {StoreId}.", store.Id);
            }
        }
    }

    private bool IsShopifyConnected(Core.Entities.Store store)
    {
        var hasOAuthConnection =
            !string.IsNullOrWhiteSpace(store.ShopifyAccessTokenEncrypted) &&
            !string.IsNullOrWhiteSpace(store.ShopifyRefreshTokenEncrypted);

        if (hasOAuthConnection)
        {
            return true;
        }

        var legacyClientId =
            _configuration[$"Shopify:Stores:{store.Id}:ClientId"];

        var legacyClientSecret =
            _configuration[$"Shopify:Stores:{store.Id}:ClientSecret"];

        var hasLegacyConnection =
            !string.IsNullOrWhiteSpace(legacyClientId) &&
            !string.IsNullOrWhiteSpace(legacyClientSecret);

        return hasLegacyConnection;
    }
}