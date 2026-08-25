using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Orders;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stores/{storeId:int}/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IStoreAccessService _storeAccessService;

    public OrdersController(
        AppDbContext dbContext,
        IStoreAccessService storeAccessService)
    {
        _dbContext = dbContext;
        _storeAccessService = storeAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderListItemDto>>> GetOrders(
        int storeId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .OrderByDescending(x => x.OrderDateUtc)
            .Select(x => new OrderListItemDto
            {
                Id = x.Id,
                DisplayOrderId = x.DisplayOrderId,
                OrderDateUtc = x.OrderDateUtc,

                FullName = x.Customer.FullName,
                Phone = x.Customer.Phone,
                AddressLine1 = x.Customer.AddressLine1,
                City = x.Customer.City,

                TotalAmount = x.TotalAmount,
                Currency = x.Currency,

                TrackingNumber = x.TrackingNumber,
                OrderStatus = x.OrderStatus,

                LocationLink = x.LocationLink,
                FinalDecision = x.FinalDecision,

                OrderSource = x.OrderSource,
                InvoiceStatus = x.InvoiceStatus,

                Items = x.OrderItems
                    .OrderBy(item => item.Id)
                    .Select(item => new OrderItemDto
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        VariantName = item.VariantName,
                        SKU = item.SKU,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.LineTotal
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }
}