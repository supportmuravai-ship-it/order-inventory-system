using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Shopify.Models;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyOrderSyncService
{
    private readonly AppDbContext _dbContext;

    public ShopifyOrderSyncService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopifySyncResult> SyncAsync(int storeId, IReadOnlyCollection<ShopifyOrder> shopifyOrders, CancellationToken cancellationToken = default)
    {
        var result = new ShopifySyncResult
        {
            Fetched = shopifyOrders.Count
        };

        foreach (var shopifyOrder in shopifyOrders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var existingOrder = await _dbContext.Orders
                    .Include(x => x.Customer)
                    .Include(x => x.OrderItems)
                    .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ExternalOrderId == shopifyOrder.Id, cancellationToken);

                if (existingOrder is null)
                {
                    CreateOrder(storeId, shopifyOrder);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    result.Created++;
                }
                else
                {
                    UpdateOrder(existingOrder, shopifyOrder);

                    _dbContext.ChangeTracker.DetectChanges();

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        var now = DateTime.UtcNow;

                        existingOrder.UpdatedAtUtc = now;

                        if (_dbContext.Entry(existingOrder.Customer).State == EntityState.Modified)
                        {
                            existingOrder.Customer.UpdatedAtUtc = now;
                        }

                        foreach (var item in existingOrder.OrderItems.Where(x => _dbContext.Entry(x).State == EntityState.Modified))
                        {
                            item.UpdatedAtUtc = now;
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        result.Updated++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"{shopifyOrder.Name}: {ex.Message}");

                _dbContext.ChangeTracker.Clear();
            }
        }

        return result;
    }

    private void CreateOrder(int storeId, ShopifyOrder shopifyOrder)
    {
        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            StoreId = storeId,
            ExternalCustomerId = shopifyOrder.Customer?.Id,
            FullName = GetCustomerName(shopifyOrder),
            Phone = GetCustomerPhone(shopifyOrder),
            AddressLine1 = GetCustomAttribute(shopifyOrder, "Full Address")
    ?? shopifyOrder.ShippingAddress?.Address1
    ?? string.Empty,

            City = GetCustomAttribute(shopifyOrder, "Emirates")
    ?? shopifyOrder.ShippingAddress?.City
    ?? string.Empty,
            Country = shopifyOrder.ShippingAddress?.Country ?? "UAE",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var order = new Order
        {
            StoreId = storeId,
            ExternalOrderId = shopifyOrder.Id,
            DisplayOrderId = shopifyOrder.Name,
            OrderSource = OrderSource.Shopify,
            Customer = customer,
            OrderStatus = OrderStatus.New,
            TrackingNumber = null,
            LocationLink = null,
            FinalDecision = null,
            InvoiceStatus = InvoiceStatus.Unpaid,
            TotalAmount = ParseMoney(shopifyOrder.TotalPriceSet.ShopMoney.Amount),
            Currency = shopifyOrder.TotalPriceSet.ShopMoney.CurrencyCode,
            OrderDateUtc = shopifyOrder.CreatedAt,
            WarehouseLocationId = null,
            LastStatusChangedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var shopifyItem in shopifyOrder.LineItems.Edges.Select(x => x.Node))
        {
            order.OrderItems.Add(CreateOrderItem(shopifyItem, now));
        }

        _dbContext.Orders.Add(order);
    }

    private void UpdateOrder(Order order, ShopifyOrder shopifyOrder)
    {
        var now = DateTime.UtcNow;

        // Shopify-owned/imported fields only.
        order.DisplayOrderId = shopifyOrder.Name;
        order.OrderSource = OrderSource.Shopify;
        order.OrderDateUtc = shopifyOrder.CreatedAt;
        order.TotalAmount = ParseMoney(shopifyOrder.TotalPriceSet.ShopMoney.Amount);
        order.Currency = shopifyOrder.TotalPriceSet.ShopMoney.CurrencyCode;

        UpdateCustomer(order.Customer, shopifyOrder);
        UpdateOrderItems(order, shopifyOrder, now);

        // Deliberately NOT changing:
        // OrderStatus
        // TrackingNumber
        // LocationLink
        // FinalDecision
        // InvoiceStatus
        // LastStatusChangedAtUtc
        // Notes
        // Status history
        // Tracking history
    }

    private static void UpdateCustomer(Customer customer, ShopifyOrder shopifyOrder)
    {
        customer.ExternalCustomerId = shopifyOrder.Customer?.Id;
        customer.FullName = GetCustomerName(shopifyOrder);
        customer.Phone = GetCustomerPhone(shopifyOrder);
        customer.AddressLine1 = GetCustomAttribute(shopifyOrder, "Full Address") ?? shopifyOrder.ShippingAddress?.Address1 ?? string.Empty;
        customer.City = GetCustomAttribute(shopifyOrder, "Emirates") ?? shopifyOrder.ShippingAddress?.City ?? string.Empty;
        customer.Country = shopifyOrder.ShippingAddress?.Country ?? "UAE";
    }

    private void UpdateOrderItems(Order order, ShopifyOrder shopifyOrder, DateTime now)
    {
        var incomingItems = shopifyOrder.LineItems.Edges.Select(x => x.Node).ToList();
        var incomingIds = incomingItems.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        var removedItems = order.OrderItems
            .Where(x => x.ExternalLineItemId is not null && !incomingIds.Contains(x.ExternalLineItemId))
            .ToList();

        if (removedItems.Count > 0)
        {
            _dbContext.OrderItems.RemoveRange(removedItems);
        }

        foreach (var shopifyItem in incomingItems)
        {
            var existingItem = order.OrderItems.FirstOrDefault(x => x.ExternalLineItemId == shopifyItem.Id);

            if (existingItem is null)
            {
                order.OrderItems.Add(CreateOrderItem(shopifyItem, now));
                continue;
            }

            existingItem.ExternalProductId = shopifyItem.Variant?.Product?.Id;
            existingItem.ExternalVariantId = shopifyItem.Variant?.Id;
            existingItem.ProductName = shopifyItem.Name;
            existingItem.VariantName = shopifyItem.Variant?.Title;
            existingItem.SKU = shopifyItem.SKU;
            existingItem.Quantity = shopifyItem.Quantity;
            existingItem.UnitPrice = ParseMoney(shopifyItem.OriginalUnitPriceSet.ShopMoney.Amount);
            existingItem.LineTotal = existingItem.UnitPrice * existingItem.Quantity;
        }
    }

    private static OrderItem CreateOrderItem(ShopifyLineItem shopifyItem, DateTime now)
    {
        var unitPrice = ParseMoney(shopifyItem.OriginalUnitPriceSet.ShopMoney.Amount);

        return new OrderItem
        {
            ExternalLineItemId = shopifyItem.Id,
            ExternalProductId = shopifyItem.Variant?.Product?.Id,
            ExternalVariantId = shopifyItem.Variant?.Id,
            ProductName = shopifyItem.Name,
            VariantName = shopifyItem.Variant?.Title,
            SKU = shopifyItem.SKU,
            Quantity = shopifyItem.Quantity,
            UnitPrice = unitPrice,
            LineTotal = unitPrice * shopifyItem.Quantity,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static string GetCustomerName(ShopifyOrder order)
    {
        var customName = GetCustomAttribute(order, "Full Name");

        if (!string.IsNullOrWhiteSpace(customName))
        {
            return customName;
        }

        var shippingName = $"{order.ShippingAddress?.FirstName} {order.ShippingAddress?.LastName}".Trim();

        if (!string.IsNullOrWhiteSpace(shippingName))
        {
            return shippingName;
        }

        var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();

        return string.IsNullOrWhiteSpace(customerName) ? "Unknown" : customerName;
    }

    private static string GetCustomerPhone(ShopifyOrder order)
    {
        var customPhone = GetCustomAttribute(order, "Contact Number");

        if (!string.IsNullOrWhiteSpace(customPhone))
        {
            return customPhone;
        }

        if (!string.IsNullOrWhiteSpace(order.ShippingAddress?.Phone))
        {
            return order.ShippingAddress.Phone;
        }

        return order.Customer?.Phone ?? string.Empty;
    }

    private static decimal ParseMoney(string amount)
    {
        if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"Invalid Shopify money amount: {amount}");
        }

        return value;
    }

    private static string? GetCustomAttribute(ShopifyOrder order, string key)
    {
        return order.CustomAttributes
            .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }
}