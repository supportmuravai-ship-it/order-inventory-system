using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShopifyReconciliationBackgroundService> _logger;

    public ShopifyReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ShopifyReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        var storeIds = await dbContext.Stores
            .AsNoTracking()
            .Where(x => x.IsActive && x.ShopDomain != null)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var storeId in storeIds)
        {
            try
            {
                var updatedSinceUtc = DateTime.UtcNow.AddMinutes(-30);

                var result = await reconciliationService.ReconcileStoreAsync(storeId, updatedSinceUtc, cancellationToken);

                _logger.LogInformation(
                    "Shopify reconciliation completed for Store {StoreId}. Fetched: {Fetched}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}.",
                    storeId,
                    result.Fetched,
                    result.Created,
                    result.Updated,
                    result.Skipped,
                    result.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shopify reconciliation failed for Store {StoreId}.", storeId);
            }
        }
    }
}