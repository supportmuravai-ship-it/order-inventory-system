using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Common;
using OrderManagement.Core.DTOs.Orders;
using OrderManagement.Core.Enums;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using System.Security.Claims;

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
    public async Task<ActionResult<PagedResultDto<OrderListItemDto>>> GetOrders(
    int storeId,
    [FromQuery] OrderQueryRequest request)
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

        if (request.Page < 1)
        {
            return BadRequest("Page must be at least 1.");
        }

        int[] allowedPageSizes = [25, 50, 100];

        if (!allowedPageSizes.Contains(request.PageSize))
        {
            return BadRequest(
                "PageSize must be 25, 50, or 100.");
        }

        if (request.OrderStatus.HasValue &&
            !Enum.IsDefined(request.OrderStatus.Value))
        {
            return BadRequest("Invalid OrderStatus.");
        }

        if (request.OrderSource.HasValue &&
            !Enum.IsDefined(request.OrderSource.Value))
        {
            return BadRequest("Invalid OrderSource.");
        }

        if (request.InvoiceStatus.HasValue &&
            !Enum.IsDefined(request.InvoiceStatus.Value))
        {
            return BadRequest("Invalid InvoiceStatus.");
        }

        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.DisplayOrderId.Contains(search) ||
                x.Customer.FullName.Contains(search) ||
                x.Customer.Phone.Contains(search) ||
                (x.TrackingNumber != null &&
                 x.TrackingNumber.Contains(search)));
        }

        if (request.DateFrom.HasValue &&
            request.DateTo.HasValue &&
            request.DateFrom.Value.Date > request.DateTo.Value.Date)
        {
            return BadRequest(
                "DateFrom cannot be later than DateTo.");
        }

        if (request.DateFrom.HasValue)
        {
            var dateFrom = request.DateFrom.Value.Date;

            query = query.Where(x =>
                x.OrderDateUtc >= dateFrom);
        }

        if (request.DateTo.HasValue)
        {
            var dateToExclusive =
                request.DateTo.Value.Date.AddDays(1);

            query = query.Where(x =>
                x.OrderDateUtc < dateToExclusive);
        }

        if (request.OrderStatus.HasValue)
        {
            query = query.Where(x =>
                x.OrderStatus == request.OrderStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Product))
        {
            var product = request.Product.Trim();

            query = query.Where(x =>
                x.OrderItems.Any(item =>
                    item.ProductName.Contains(product)));
        }

        if (!string.IsNullOrWhiteSpace(request.SKU))
        {
            var sku = request.SKU.Trim();

            query = query.Where(x =>
                x.OrderItems.Any(item =>
                    item.SKU != null &&
                    item.SKU.Contains(sku)));
        }

        if (request.OrderSource.HasValue)
        {
            query = query.Where(x =>
                x.OrderSource == request.OrderSource.Value);
        }

        if (request.InvoiceStatus.HasValue)
        {
            query = query.Where(x =>
                x.InvoiceStatus == request.InvoiceStatus.Value);
        }


        string[] allowedSortOptions =
        [
            "newest",
            "oldest",
            "orderId",
            "customerName",
            "totalPrice",
            "orderStatus"
        ];

        if (!allowedSortOptions.Contains(request.Sort))
        {
            return BadRequest(
                "Sort must be newest, oldest, orderId, customerName, totalPrice, or orderStatus.");
        }

        var totalCount = await query.CountAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)request.PageSize);



        var sortedQuery = request.Sort switch
        {
            "oldest" => query
                .OrderBy(x => x.OrderDateUtc)
                .ThenBy(x => x.Id),

            "orderId" => query
                .OrderBy(x => x.DisplayOrderId)
                .ThenBy(x => x.Id),

            "customerName" => query
                .OrderBy(x => x.Customer.FullName)
                .ThenByDescending(x => x.OrderDateUtc),

            "totalPrice" => query
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.OrderDateUtc),

            "orderStatus" => query
                .OrderBy(x => x.OrderStatus)
                .ThenByDescending(x => x.OrderDateUtc),

            _ => query
                .OrderByDescending(x => x.OrderDateUtc)
                .ThenByDescending(x => x.Id)
        };

        var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
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

        var result = new PagedResultDto<OrderListItemDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(result);
    }


    [HttpGet("summary")]
    public async Task<ActionResult<OrderSummaryDto>> GetSummary(
    int storeId,
    [FromQuery] DateTime? dateFrom,
    [FromQuery] DateTime? dateTo)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        if (dateFrom.HasValue &&
            dateTo.HasValue &&
            dateFrom.Value.Date > dateTo.Value.Date)
        {
            return BadRequest(
                "DateFrom cannot be later than DateTo.");
        }

        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.StoreId == storeId);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;

            query = query.Where(x =>
                x.OrderDateUtc >= from);
        }

        if (dateTo.HasValue)
        {
            var toExclusive =
                dateTo.Value.Date.AddDays(1);

            query = query.Where(x =>
                x.OrderDateUtc < toExclusive);
        }

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new OrderSummaryDto
            {
                TotalOrders = group.Count(),

                Confirmed = group.Count(x =>
                    x.OrderStatus == OrderStatus.Confirmed),

                Shipped = group.Count(x =>
                    x.OrderStatus == OrderStatus.Shipped),

                Delivered = group.Count(x =>
                    x.OrderStatus == OrderStatus.Delivered),

                NoResponse = group.Count(x =>
                    x.OrderStatus == OrderStatus.NoResponse),

                Return = group.Count(x =>
                    x.OrderStatus == OrderStatus.Return),

                ReturnInProcess = group.Count(x =>
                    x.OrderStatus == OrderStatus.ReturnInProcess),

                Cancelled = group.Count(x =>
                    x.OrderStatus == OrderStatus.Cancelled),

                RepeatedOrder = group.Count(x =>
                    x.OrderStatus == OrderStatus.RepeatedOrder)
            })
            .SingleOrDefaultAsync();

        return Ok(summary ?? new OrderSummaryDto());
    }


    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder( int storeId,int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync( userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Where(x =>
                x.StoreId == storeId &&
                x.Id == orderId)
            .Select(x => new OrderDetailsDto
            {
                Id = x.Id,

                DisplayOrderId = x.DisplayOrderId,

                ExternalOrderId = x.ExternalOrderId,

                OrderDateUtc = x.OrderDateUtc,

                FullName = x.Customer.FullName,

                Phone = x.Customer.Phone,

                AddressLine1 = x.Customer.AddressLine1,

                City = x.Customer.City,

                Country = x.Customer.Country,

                TotalAmount = x.TotalAmount,

                Currency = x.Currency,

                TrackingNumber = x.TrackingNumber,

                OrderStatus = x.OrderStatus,

                LocationLink = x.LocationLink,

                FinalDecision = x.FinalDecision,

                OrderSource = x.OrderSource,

                InvoiceStatus = x.InvoiceStatus,

                WarehouseName = x.WarehouseLocation != null
                    ? x.WarehouseLocation.Name
                    : null,

                LastStatusChangedAtUtc = x.LastStatusChangedAtUtc,

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
            .SingleOrDefaultAsync();

        if (order is null)
        {
            return NotFound();
        }

        order.OrderDateUtc = DateTime.SpecifyKind( order.OrderDateUtc, DateTimeKind.Utc);

        order.LastStatusChangedAtUtc = DateTime.SpecifyKind( order.LastStatusChangedAtUtc, DateTimeKind.Utc);

        return Ok(order);
    }

    [HttpPut("{orderId:int}/status")]
    [Authorize(Roles = "Admin,CustomerSupport,WarehouseStaff")]
    public async Task<IActionResult> UpdateOrderStatus(
    int storeId,
    int orderId,
    UpdateOrderStatusRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        if (!Enum.IsDefined(request.OrderStatus))
        {
            return BadRequest("Invalid OrderStatus.");
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        // Submitting the existing status is not a real status change.
        if (order.OrderStatus == request.OrderStatus)
        {
            return NoContent();
        }

        var now = DateTime.UtcNow;

        order.OrderStatus = request.OrderStatus;
        order.LastStatusChangedAtUtc = now;
        order.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{orderId:int}/tracking")]
    [Authorize(Roles = "Admin,WarehouseStaff")]
    public async Task<IActionResult> UpdateTracking(
    int storeId,
    int orderId,
    UpdateTrackingRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var trackingNumber =
            string.IsNullOrWhiteSpace(request.TrackingNumber)
                ? null
                : request.TrackingNumber.Trim();

        if (trackingNumber is not null &&
            trackingNumber.Length > 100)
        {
            return BadRequest(
                "Tracking Number cannot exceed 100 characters.");
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.TrackingNumber == trackingNumber)
        {
            return NoContent();
        }

        order.TrackingNumber = trackingNumber;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent(); // The request was successful, and there's no response body to return
    }


    [HttpPut("{orderId:int}/invoice-status")]
    [Authorize(Roles = "Admin,CustomerSupport")]
    public async Task<IActionResult> UpdateInvoiceStatus(
    int storeId,
    int orderId,
    UpdateInvoiceStatusRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        if (!Enum.IsDefined(request.InvoiceStatus))
        {
            return BadRequest("Invalid InvoiceStatus.");
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.InvoiceStatus == request.InvoiceStatus)
        {
            return NoContent();
        }

        order.InvoiceStatus = request.InvoiceStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{orderId:int}/location-link")]
    [Authorize(Roles = "Admin,CustomerSupport")]
    public async Task<IActionResult> UpdateLocationLink(
    int storeId,
    int orderId,
    UpdateLocationLinkRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var locationLink =
            string.IsNullOrWhiteSpace(request.LocationLink)
                ? null
                : request.LocationLink.Trim();

        if (locationLink is not null)
        {
            if (locationLink.Length > 500)
            {
                return BadRequest(
                    "Location Link cannot exceed 500 characters.");
            }

            if (!Uri.TryCreate(
                    locationLink,
                    UriKind.Absolute,
                    out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(
                    "Location Link must be a valid http or https URL.");
            }
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.LocationLink == locationLink)
        {
            return NoContent();
        }

        order.LocationLink = locationLink;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{orderId:int}/final-decision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFinalDecision(
    int storeId,
    int orderId,
    UpdateFinalDecisionRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var finalDecision =
            string.IsNullOrWhiteSpace(request.FinalDecision)
                ? null
                : request.FinalDecision.Trim();

        if (finalDecision is not null &&
            finalDecision.Length > 500)
        {
            return BadRequest(
                "Final Decision cannot exceed 500 characters.");
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.FinalDecision == finalDecision)
        {
            return NoContent();
        }

        order.FinalDecision = finalDecision;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}