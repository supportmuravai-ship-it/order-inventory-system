using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Common;
using OrderManagement.Core.DTOs.Orders;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stores/{storeId:int}/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IStoreAccessService _storeAccessService;

    private readonly ICsvOrderImportService _csvOrderImportService;

    public OrdersController(
    AppDbContext dbContext,
    IStoreAccessService storeAccessService,
    ICsvOrderImportService csvOrderImportService)
    {
        _dbContext = dbContext;
        _storeAccessService = storeAccessService;
        _csvOrderImportService = csvOrderImportService;
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

        if (request.NeedToShip.HasValue)
        {
            query = query.Where(x =>
                x.NeedToShip == request.NeedToShip.Value);
        }

        if (request.NeedsAttention == true)
        {
            var attentionThreshold =
                DateTime.UtcNow.AddHours(-12);

            query = query.Where(x =>
                x.OrderStatus != OrderStatus.Delivered &&
                x.OrderStatus != OrderStatus.Return &&
                x.OrderStatus != OrderStatus.Cancelled &&
                x.OrderStatus != OrderStatus.RepeatedOrder &&
                x.LastStatusChangedAtUtc <= attentionThreshold);
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
                .ThenBy(x => x.CreatedAtUtc)
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
                .ThenByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
        };

        var now = DateTime.UtcNow;

        var orderRows = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                Order = new OrderListItemDto
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
                    AirwayBillUrl = x.AirwayBillUrl,
                    OrderStatus = x.OrderStatus,
                    NeedToShip = x.NeedToShip,

                    LocationLink = x.LocationLink,
                    FinalDecision = x.FinalDecision,

                    OrderSource = x.OrderSource,
                    InvoiceStatus = x.InvoiceStatus,

                    ShoaibNote = x.Notes
                        .Where(note =>
                            note.NoteType == NoteType.Shoaib)
                        .Select(note => note.Text)
                        .FirstOrDefault(),

                    TrenvoNote = x.Notes
                        .Where(note =>
                            note.NoteType == NoteType.Trenvo)
                        .Select(note => note.Text)
                        .FirstOrDefault(),

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
                },

                x.LastStatusChangedAtUtc
            })
            .ToListAsync();

        foreach (var row in orderRows)
        {
            var hoursInCurrentStatus =
                (now - row.LastStatusChangedAtUtc).TotalHours;

            row.Order.HoursInCurrentStatus =
                Math.Max(0, hoursInCurrentStatus);

            var isFinalStatus =
                row.Order.OrderStatus == OrderStatus.Delivered ||
                row.Order.OrderStatus == OrderStatus.Return ||
                row.Order.OrderStatus == OrderStatus.Cancelled ||
                row.Order.OrderStatus == OrderStatus.RepeatedOrder;

            row.Order.NeedsAttention =
                !isFinalStatus &&
                hoursInCurrentStatus >= 12;
        }

        var items = orderRows
            .Select(x => x.Order)
            .ToList();

        // query --> Orders + filters

        //sortedQuery --> query + sorting       

        //orderRows --> sortedQuery + pagination + selected columns

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

        var attentionThreshold = DateTime.UtcNow.AddHours(-12);

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
                    x.OrderStatus == OrderStatus.RepeatedOrder),

                NeedsAttention = group.Count(x =>
                    x.OrderStatus != OrderStatus.Delivered &&
                    x.OrderStatus != OrderStatus.Return &&
                    x.OrderStatus != OrderStatus.Cancelled &&
                    x.OrderStatus != OrderStatus.RepeatedOrder &&
                    x.LastStatusChangedAtUtc <= attentionThreshold),

                NeedToShip = group.Count(x =>
                    x.NeedToShip),

                New = group.Count(x =>
                    x.OrderStatus == OrderStatus.New),
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
            .AsSplitQuery()
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

                AirwayBillUrl = x.AirwayBillUrl,

                OrderStatus = x.OrderStatus,

                NeedToShip = x.NeedToShip,

                LocationLink = x.LocationLink,

                FinalDecision = x.FinalDecision,

                CancellationReturnReason = x.CancellationReturnReason,

                CancellationReturnEvidenceUrl = x.CancellationReturnEvidenceUrl,

                OrderSource = x.OrderSource,

                InvoiceStatus = x.InvoiceStatus,

                ShoaibNote = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Shoaib)
                    .Select(note => note.Text)
                    .FirstOrDefault(),

                ShoaibNoteUpdatedAtUtc = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Shoaib)
                    .Select(note => (DateTime?)note.UpdatedAtUtc)
                    .FirstOrDefault(),

                ShoaibNoteUpdatedBy = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Shoaib)
                    .Select(note =>
                        _dbContext.Users
                            .Where(user =>
                                user.Id == note.UpdatedByUserId)
                            .Select(user =>
                                user.Email ??
                                user.UserName ??
                                user.Id)
                            .FirstOrDefault() ??
                        note.UpdatedByUserId)
                    .FirstOrDefault(),

                ShoaibNoteHistory = x.NoteHistory
                    .Where(history => history.NoteType == NoteType.Shoaib)
                    .OrderByDescending(history => history.ChangedAtUtc)
                    .Select(history => new OrderNoteHistoryDto
                    {
                        NoteType = history.NoteType,
                        OldText = history.OldText,
                        NewText = history.NewText,
                        ChangedByUserId = history.ChangedByUserId,

                        ChangedBy = _dbContext.Users
                            .Where(user => user.Id == history.ChangedByUserId)
                            .Select(user => user.Email ?? user.UserName ?? user.Id)
                            .FirstOrDefault() ?? history.ChangedByUserId,

                        ChangedAtUtc = history.ChangedAtUtc
                    })
                    .ToList(),


                TrenvoNote = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Trenvo)
                    .Select(note => note.Text)
                    .FirstOrDefault(),

                 TrenvoNoteUpdatedAtUtc = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Trenvo)
                    .Select(note => (DateTime?)note.UpdatedAtUtc)
                    .FirstOrDefault(),

                 TrenvoNoteUpdatedBy = x.Notes
                    .Where(note =>
                        note.NoteType == NoteType.Trenvo)
                    .Select(note =>
                        _dbContext.Users
                            .Where(user =>
                                user.Id == note.UpdatedByUserId)
                            .Select(user =>
                                user.Email ??
                                user.UserName ??
                                user.Id)
                            .FirstOrDefault() ??
                        note.UpdatedByUserId)
                    .FirstOrDefault(),

                TrenvoNoteHistory = x.NoteHistory
                    .Where(history => history.NoteType == NoteType.Trenvo)
                    .OrderByDescending(history => history.ChangedAtUtc)
                    .Select(history => new OrderNoteHistoryDto
                    {
                        NoteType = history.NoteType,
                        OldText = history.OldText,
                        NewText = history.NewText,
                        ChangedByUserId = history.ChangedByUserId,

                        ChangedBy = _dbContext.Users
                            .Where(user => user.Id == history.ChangedByUserId)
                            .Select(user => user.Email ?? user.UserName ?? user.Id)
                            .FirstOrDefault() ?? history.ChangedByUserId,

                        ChangedAtUtc = history.ChangedAtUtc
                    })
                    .ToList(),

                WarehouseName = x.WarehouseLocation != null
                    ? x.WarehouseLocation.Name
                    : null,

                LastStatusChangedAtUtc = x.LastStatusChangedAtUtc,

                StatusHistory = x.StatusHistory
                    .OrderByDescending(history => history.ChangedAtUtc)
                    .Select(history => new OrderStatusHistoryDto
                    {
                        OldStatus = history.OldStatus,
                        NewStatus = history.NewStatus,
                        ChangedByUserId = history.ChangedByUserId,

                        ChangedBy = _dbContext.Users
                            .Where(user => user.Id == history.ChangedByUserId)
                            .Select(user => user.Email ?? user.UserName ?? user.Id)
                            .FirstOrDefault() ?? history.ChangedByUserId,

                        ChangedAtUtc = history.ChangedAtUtc
                    })
                    .ToList(),

                TrackingHistory = x.TrackingHistory
                    .OrderByDescending(history => history.ChangedAtUtc)
                    .Select(history => new TrackingHistoryDto
                    {
                        OldTrackingNumber = history.OldTrackingNumber,
                        NewTrackingNumber = history.NewTrackingNumber,
                        ChangedByUserId = history.ChangedByUserId,

                        ChangedBy = _dbContext.Users
                            .Where(user => user.Id == history.ChangedByUserId)
                            .Select(user => user.Email ?? user.UserName ?? user.Id)
                            .FirstOrDefault() ?? history.ChangedByUserId,

                        ChangedAtUtc = history.ChangedAtUtc
                    })
                    .ToList(),

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

        foreach (var history in order.StatusHistory)
        {
            history.ChangedAtUtc = DateTime.SpecifyKind(
                history.ChangedAtUtc,
                DateTimeKind.Utc);
        }

        foreach (var history in order.TrackingHistory)
        {
            history.ChangedAtUtc = DateTime.SpecifyKind(
                history.ChangedAtUtc,
                DateTimeKind.Utc);
        }

        foreach (var history in order.ShoaibNoteHistory)
        {
            history.ChangedAtUtc = DateTime.SpecifyKind(
                history.ChangedAtUtc,
                DateTimeKind.Utc);
        }

        foreach (var history in order.TrenvoNoteHistory)
        {
            history.ChangedAtUtc = DateTime.SpecifyKind(
                history.ChangedAtUtc,
                DateTimeKind.Utc);
        }

        if (order.ShoaibNoteUpdatedAtUtc.HasValue)
        {
            order.ShoaibNoteUpdatedAtUtc =
                DateTime.SpecifyKind(
                    order.ShoaibNoteUpdatedAtUtc.Value,
                    DateTimeKind.Utc);
        }

        if (order.TrenvoNoteUpdatedAtUtc.HasValue)
        {
            order.TrenvoNoteUpdatedAtUtc =
                DateTime.SpecifyKind(
                    order.TrenvoNoteUpdatedAtUtc.Value,
                    DateTimeKind.Utc);
        }

        var now = DateTime.UtcNow;

        var hoursInCurrentStatus =
            (now - order.LastStatusChangedAtUtc).TotalHours;

        order.HoursInCurrentStatus =
            Math.Max(0, hoursInCurrentStatus);

        var isFinalStatus =
            order.OrderStatus == OrderStatus.Delivered ||
            order.OrderStatus == OrderStatus.Return ||
            order.OrderStatus == OrderStatus.Cancelled ||
            order.OrderStatus == OrderStatus.RepeatedOrder;

        order.NeedsAttention =
            !isFinalStatus &&
            hoursInCurrentStatus >= 12;

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

        var requiresCancellationDetails =
    request.OrderStatus == OrderStatus.Cancelled ||
    request.OrderStatus == OrderStatus.Return;

        if (requiresCancellationDetails)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest("A reason is required when cancelling or returning an order.");
            }

            if (string.IsNullOrWhiteSpace(request.EvidenceUrl))
            {
                return BadRequest("An evidence image link is required when cancelling or returning an order.");
            }

            if (!Uri.TryCreate(request.EvidenceUrl.Trim(), UriKind.Absolute, out var evidenceUri) ||
                evidenceUri.Scheme != Uri.UriSchemeHttps)
            {
                return BadRequest("Evidence link must be a valid HTTPS URL.");
            }
        }
        var now = DateTime.UtcNow;

        var statusHistory = new OrderStatusHistory
        {
            OrderId = order.Id,
            OldStatus = order.OrderStatus,
            NewStatus = request.OrderStatus,
            ChangedByUserId = userId,
            ChangedAtUtc = now
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        _dbContext.OrderStatusHistories.Add(statusHistory);

        order.OrderStatus = request.OrderStatus;

        if (requiresCancellationDetails)
        {
            order.CancellationReturnReason = request.Reason!.Trim();
            order.CancellationReturnEvidenceUrl = request.EvidenceUrl!.Trim();
        }

        if (request.OrderStatus != OrderStatus.New &&
            request.OrderStatus != OrderStatus.Confirmed)
        {
            order.NeedToShip = false;
        }

        order.LastStatusChangedAtUtc = now;
        order.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

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

        var now = DateTime.UtcNow;

        var trackingHistory = new TrackingHistory
        {
            OrderId = order.Id,
            OldTrackingNumber = order.TrackingNumber,
            NewTrackingNumber = trackingNumber,
            ChangedByUserId = userId,
            ChangedAtUtc = now
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        _dbContext.TrackingHistories.Add(trackingHistory);

        order.TrackingNumber = trackingNumber;
        order.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

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
    [Authorize(Roles = "Admin,CustomerSupport,WarehouseStaff")]
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


    [HttpPut("{orderId:int}/shoaib-note")]
    [Authorize(Roles = "Admin,CustomerSupport")]
    public async Task<IActionResult> UpdateShoaibNote(
    int storeId,
    int orderId,
    UpdateOrderNoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var noteText = string.IsNullOrWhiteSpace(request.Text)
            ? null
            : request.Text.Trim();

        if (noteText is not null && noteText.Length > 2000)
        {
            return BadRequest("Shoaib's Note cannot exceed 2000 characters.");
        }

        var orderExists = await _dbContext.Orders
            .AnyAsync(x => x.StoreId == storeId && x.Id == orderId);

        if (!orderExists)
        {
            return NotFound();
        }

        var existingNote = await _dbContext.OrderNotes
            .SingleOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.NoteType == NoteType.Shoaib);

        if (existingNote is null && noteText is null)
        {
            return NoContent();
        }

        if (existingNote is not null && existingNote.Text == noteText)
        {
            return NoContent();
        }

        var now = DateTime.UtcNow;

        var history = new OrderNoteHistory
        {
            OrderId = orderId,
            NoteType = NoteType.Shoaib,
            OldText = existingNote?.Text,
            NewText = noteText,
            ChangedByUserId = userId,
            ChangedAtUtc = now
        };

        _dbContext.OrderNoteHistories.Add(history);

        if (noteText is null)
        {
            _dbContext.OrderNotes.Remove(existingNote!);
        }
        else if (existingNote is null)
        {
            var note = new OrderNote
            {
                OrderId = orderId,
                NoteType = NoteType.Shoaib,
                Text = noteText,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.OrderNotes.Add(note);
        }
        else
        {
            existingNote.Text = noteText;
            existingNote.UpdatedByUserId = userId;
            existingNote.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{orderId:int}/trenvo-note")]
    [Authorize(Roles = "Admin,WarehouseStaff")]
    public async Task<IActionResult> UpdateTrenvoNote(
    int storeId,
    int orderId,
    UpdateOrderNoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var noteText = string.IsNullOrWhiteSpace(request.Text)
            ? null
            : request.Text.Trim();

        if (noteText is not null && noteText.Length > 2000)
        {
            return BadRequest("Trenvo Note cannot exceed 2000 characters.");
        }

        var orderExists = await _dbContext.Orders
            .AnyAsync(x => x.StoreId == storeId && x.Id == orderId);

        if (!orderExists)
        {
            return NotFound();
        }

        var existingNote = await _dbContext.OrderNotes
            .SingleOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.NoteType == NoteType.Trenvo);

        if (existingNote is null && noteText is null)
        {
            return NoContent();
        }

        if (existingNote is not null && existingNote.Text == noteText)
        {
            return NoContent();
        }

        var now = DateTime.UtcNow;

        var history = new OrderNoteHistory
        {
            OrderId = orderId,
            NoteType = NoteType.Trenvo,
            OldText = existingNote?.Text,
            NewText = noteText,
            ChangedByUserId = userId,
            ChangedAtUtc = now
        };

        _dbContext.OrderNoteHistories.Add(history);

        if (noteText is null)
        {
            _dbContext.OrderNotes.Remove(existingNote!);
        }
        else if (existingNote is null)
        {
            var note = new OrderNote
            {
                OrderId = orderId,
                NoteType = NoteType.Trenvo,
                Text = noteText,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.OrderNotes.Add(note);
        }
        else
        {
            existingNote.Text = noteText;
            existingNote.UpdatedByUserId = userId;
            existingNote.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin,CustomerSupport")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<CsvImportResultDto>> ImportCsv(
    int storeId,
    IFormFile file,
    CancellationToken cancellationToken)
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

        if (file is null || file.Length == 0)
        {
            return BadRequest(
                "Please select a CSV file.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(
                "CSV file cannot exceed 5 MB.");
        }

        var extension =
            Path.GetExtension(file.FileName);

        if (!string.Equals(
                extension,
                ".csv",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Only CSV files are allowed.");
        }

        await using var stream =
            file.OpenReadStream();

        var result =
            await _csvOrderImportService.ImportAsync(
                storeId,
                stream,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CustomerSupport")]
    public async Task<IActionResult> CreateManualOrder(
    int storeId,
    CreateManualOrderRequest request,
    CancellationToken cancellationToken)
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

        var displayOrderId =
            request.DisplayOrderId?.Trim();

        if (string.IsNullOrWhiteSpace(displayOrderId))
        {
            return BadRequest(
                "Display Order ID is required.");
        }

        if (displayOrderId.Length > 100)
        {
            return BadRequest(
                "Display Order ID cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(
                "Customer Full Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(
                "Phone is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            return BadRequest(
                "Address 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(
                "City is required.");
        }

        if (request.OrderDateUtc == default)
        {
            return BadRequest(
                "Order Date is required.");
        }

        /*
         * Current OrderSource enum contains:
         * Shopify, CSVImport, WhatsApp, Other.
         *
         * Manual creation should only use WhatsApp or Other.
         */
        if (request.OrderSource != OrderSource.WhatsApp &&
            request.OrderSource != OrderSource.Other)
        {
            return BadRequest(
                "Manual orders can only use WhatsApp or Other as Order Source.");
        }

        if (request.TotalAmount < 0)
        {
            return BadRequest(
                "Total Amount cannot be negative.");
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            return BadRequest(
                "At least one order item is required.");
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                return BadRequest(
                    "Product Name is required for every item.");
            }

            if (item.Quantity <= 0)
            {
                return BadRequest(
                    "Quantity must be greater than 0.");
            }

            if (item.UnitPrice < 0)
            {
                return BadRequest(
                    "Unit Price cannot be negative.");
            }
        }

        var duplicateExists =
            await _dbContext.Orders.AnyAsync(
                x =>
                    x.StoreId == storeId &&
                    x.DisplayOrderId == displayOrderId,
                cancellationToken);

        if (duplicateExists)
        {
            return Conflict(
                $"Order ID {displayOrderId} already exists for this store.");
        }

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            StoreId = storeId,

            ExternalCustomerId = null,

            FullName = request.FullName.Trim(),

            Phone = request.Phone.Trim(),

            AddressLine1 =
                request.AddressLine1.Trim(),

            City = request.City.Trim(),

            Country = "UAE",

            CreatedAtUtc = now,

            UpdatedAtUtc = now
        };

        var order = new Order
        {
            StoreId = storeId,

            ExternalOrderId = null,

            DisplayOrderId = displayOrderId,

            OrderSource = request.OrderSource,

            Customer = customer,

            OrderStatus = OrderStatus.New,

            TrackingNumber = null,

            LocationLink = null,

            FinalDecision = null,

            InvoiceStatus = InvoiceStatus.Unpaid,

            TotalAmount = request.TotalAmount,

            Currency = "AED",

            OrderDateUtc =
                request.OrderDateUtc.Kind == DateTimeKind.Utc
                    ? request.OrderDateUtc
                    : request.OrderDateUtc.ToUniversalTime(),

            WarehouseLocationId = null,

            LastStatusChangedAtUtc = now,

            CreatedAtUtc = now,

            UpdatedAtUtc = now
        };

        foreach (var requestItem in request.Items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductName =
                    requestItem.ProductName.Trim(),

                VariantName =
                    string.IsNullOrWhiteSpace(
                        requestItem.VariantName)
                        ? null
                        : requestItem.VariantName.Trim(),

                SKU =
                    string.IsNullOrWhiteSpace(
                        requestItem.SKU)
                        ? null
                        : requestItem.SKU.Trim(),

                Quantity = requestItem.Quantity,

                UnitPrice = requestItem.UnitPrice,

                LineTotal =
                    requestItem.Quantity *
                    requestItem.UnitPrice,

                CreatedAtUtc = now,

                UpdatedAtUtc = now
            });
        }

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreatedAtAction(
            nameof(GetOrder),
            new
            {
                storeId,
                orderId = order.Id
            },
            new
            {
                order.Id,
                order.DisplayOrderId
            });
    }

    [HttpPut("{orderId:int}/need-to-ship")]
    [Authorize(Roles = "Admin,CustomerSupport,WarehouseStaff")]
    public async Task<IActionResult> UpdateNeedToShip(
    int storeId,
    int orderId,
    UpdateNeedToShipRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var order = await _dbContext.Orders
            .SingleOrDefaultAsync(x =>
                x.StoreId == storeId &&
                x.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (request.NeedToShip &&
            order.OrderStatus != OrderStatus.New &&
            order.OrderStatus != OrderStatus.Confirmed)
        {
            return BadRequest(
                "Need To Ship can only be enabled for New or Confirmed orders.");
        }

        if (order.NeedToShip == request.NeedToShip)
        {
            return NoContent();
        }

        order.NeedToShip = request.NeedToShip;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{orderId:int}/airway-bill")]
    [Authorize(Roles = "Admin,WarehouseStaff")]
    public async Task<IActionResult> UpdateAirwayBill(
    int storeId,
    int orderId,
    UpdateAirwayBillRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasAccess)
        {
            return Forbid();
        }

        string? airwayBillUrl = null;

        if (!string.IsNullOrWhiteSpace(request.AirwayBillUrl))
        {
            airwayBillUrl = request.AirwayBillUrl.Trim();

            if (!Uri.TryCreate(airwayBillUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return BadRequest("Airway Bill link must be a valid HTTPS URL.");
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

        order.AirwayBillUrl = airwayBillUrl;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}