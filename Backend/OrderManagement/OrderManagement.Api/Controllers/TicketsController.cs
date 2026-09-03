using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Common;
using OrderManagement.Core.DTOs.Tickets;
using OrderManagement.Core.Enums;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using OrderManagement.Core.Entities;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStoreAccessService _storeAccessService;

    public TicketsController(
        AppDbContext db,
        IStoreAccessService storeAccessService)
    {
        _db = db;
        _storeAccessService = storeAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TicketListItemDto>>> GetTickets(
        [FromQuery] int storeId,
        [FromQuery] TicketQueryRequest request)
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

        if (request.Page < 1)
        {
            return BadRequest("Page must be at least 1.");
        }

        int[] allowedPageSizes = [25, 50, 100];

        if (!allowedPageSizes.Contains(request.PageSize))
        {
            return BadRequest("PageSize must be 25, 50, or 100.");
        }

        if (request.Status.HasValue &&
            !Enum.IsDefined(request.Status.Value))
        {
            return BadRequest("Invalid ticket status.");
        }

        var isAdmin = User.IsInRole("Admin");

        var query = _db.OrderTickets
    .AsNoTracking()
    .Where(x => x.StoreId == storeId);

        if (!isAdmin)
        {
            query = query.Where(x =>
                x.AssignedToUserId == userId ||
                x.CreatedByUserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            query = query.Where(x =>
                x.AssignedToUserId == request.AssignedToUserId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Title.Contains(search) ||
                (x.Order != null &&
                 x.Order.DisplayOrderId.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new TicketListItemDto
            {
                Id = x.Id,

                OrderId = x.OrderId,
                DisplayOrderId = x.Order != null ? x.Order.DisplayOrderId : null,

                AssignedToUserId = x.AssignedToUserId,
                AssignedToEmail = _db.Users
                    .Where(user => user.Id == x.AssignedToUserId)
                    .Select(user => user.Email ?? "")
                    .FirstOrDefault()!,

                CreatedByUserId = x.CreatedByUserId,
                CreatedByEmail = _db.Users
                    .Where(user => user.Id == x.CreatedByUserId)
                    .Select(user => user.Email ?? "")
                    .FirstOrDefault()!,

                Title = x.Title,
                Status = x.Status,
                CreatedAtUtc = DateTime.SpecifyKind(
    x.CreatedAtUtc,
    DateTimeKind.Utc),

                ClosedAtUtc = x.ClosedAtUtc.HasValue
    ? DateTime.SpecifyKind(
        x.ClosedAtUtc.Value,
        DateTimeKind.Utc)
    : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<TicketListItemDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize)
        });
    }

    [HttpGet("my-open-count")]
    public async Task<ActionResult<int>> GetMyOpenCount(
        [FromQuery] int storeId)
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

        var count = await _db.OrderTickets
    .AsNoTracking()
    .CountAsync(x =>
        x.StoreId == storeId &&
        x.AssignedToUserId == userId &&
        x.Status == TicketStatus.Open);

        return Ok(count);
    }

    [HttpGet("assignable-users")]
    public async Task<ActionResult<List<AssignableTicketUserDto>>> GetAssignableUsers(
    [FromQuery] int storeId)
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

        var users = await (
            from access in _db.UserStoreAccesses
            join user in _db.Users on access.UserId equals user.Id
            where access.StoreId == storeId
            orderby user.Email
            select new
            {
                user.Id,
                Email = user.Email ?? ""
            })
            .AsNoTracking()
            .ToListAsync();

        var result = new List<AssignableTicketUserDto>();

        foreach (var user in users)
        {
            var roles = await (
                from userRole in _db.UserRoles
                join role in _db.Roles on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id
                select role.Name!)
                .ToListAsync();

            var canReceiveTickets =
    roles.Contains("Admin") ||
    roles.Contains("CustomerSupport") ||
    roles.Contains("WarehouseStaff");

            if (!canReceiveTickets)
            {
                continue;
            }

            result.Add(new AssignableTicketUserDto
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles
            });
        }

        return Ok(result);
    }

    [HttpPost("/api/orders/{orderId:int}/tickets")]
    public async Task<ActionResult> CreateTicket(
    int orderId,
    [FromBody] CreateTicketRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var canCreate =
    User.IsInRole("Admin") ||
    User.IsInRole("CustomerSupport") ||
    User.IsInRole("WarehouseStaff");

        if (!canCreate)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            return BadRequest("Assigned user is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var title = request.Title.Trim();
        var message = request.Message.Trim();

        if (title.Length > 200)
        {
            return BadRequest("Title cannot exceed 200 characters.");
        }

        if (message.Length > 4000)
        {
            return BadRequest("Message cannot exceed 4000 characters.");
        }

        var order = await _db.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId)
            .Select(x => new
            {
                x.Id,
                x.StoreId
            })
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return NotFound("Order not found.");
        }

        var hasStoreAccess = await _storeAccessService.HasAccessAsync(
            userId,
            order.StoreId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        var assigneeHasStoreAccess = await _db.UserStoreAccesses
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == request.AssignedToUserId &&
                x.StoreId == order.StoreId);

        if (!assigneeHasStoreAccess)
        {
            return BadRequest(
                "Assigned user does not have access to this order's store.");
        }

        var assigneeRoles = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == request.AssignedToUserId
            select role.Name!)
            .ToListAsync();

        var assigneeCanReceive =
    assigneeRoles.Contains("Admin") ||
    assigneeRoles.Contains("CustomerSupport") ||
    assigneeRoles.Contains("WarehouseStaff");

        if (!assigneeCanReceive)
        {
            return BadRequest(
                "Selected user cannot receive order tickets.");
        }

        var ticket = new OrderTicket
        {
            StoreId = order.StoreId,
            OrderId = order.Id,
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = userId,
            Title = title,
            Message = message,
            Status = TicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.OrderTickets.Add(ticket);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ticket.Id
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailsDto>> GetTicket(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ticket = await _db.OrderTickets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Ticket = x,
                StoreId = x.StoreId,
                DisplayOrderId = x.Order != null ? x.Order.DisplayOrderId : null
            })
            .FirstOrDefaultAsync();

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(
            userId,
            ticket.StoreId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin &&
    ticket.Ticket.AssignedToUserId != userId &&
    ticket.Ticket.CreatedByUserId != userId)
        {
            return Forbid();
        }

        var result = new TicketDetailsDto
        {
            Id = ticket.Ticket.Id,

            OrderId = ticket.Ticket.OrderId,
            DisplayOrderId = ticket.DisplayOrderId,

            AssignedToUserId = ticket.Ticket.AssignedToUserId,
            AssignedToEmail = await _db.Users
                .Where(x => x.Id == ticket.Ticket.AssignedToUserId)
                .Select(x => x.Email ?? "")
                .FirstOrDefaultAsync() ?? "",

            CreatedByUserId = ticket.Ticket.CreatedByUserId,
            CreatedByEmail = await _db.Users
                .Where(x => x.Id == ticket.Ticket.CreatedByUserId)
                .Select(x => x.Email ?? "")
                .FirstOrDefaultAsync() ?? "",

            ClosedByUserId = ticket.Ticket.ClosedByUserId,

            Title = ticket.Ticket.Title,
            Message = ticket.Ticket.Message,

            Status = ticket.Ticket.Status,
            CreatedAtUtc = DateTime.SpecifyKind(
    ticket.Ticket.CreatedAtUtc,
    DateTimeKind.Utc),

            ClosedAtUtc = ticket.Ticket.ClosedAtUtc.HasValue
    ? DateTime.SpecifyKind(
        ticket.Ticket.ClosedAtUtc.Value,
        DateTimeKind.Utc)
    : null
        };

        if (!string.IsNullOrWhiteSpace(ticket.Ticket.ClosedByUserId))
        {
            result.ClosedByEmail = await _db.Users
                .Where(x => x.Id == ticket.Ticket.ClosedByUserId)
                .Select(x => x.Email ?? "")
                .FirstOrDefaultAsync();
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult> CloseTicket(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ticket = await _db.OrderTickets
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        var hasAccess = await _storeAccessService.HasAccessAsync(
    userId,
    ticket.StoreId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin &&
            ticket.AssignedToUserId != userId &&
            ticket.CreatedByUserId != userId)
        {
            return Forbid();
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return BadRequest("Ticket is already closed.");
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.ClosedByUserId = userId;

        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPatch("{id:int}/assignment")]
    public async Task<ActionResult> UpdateAssignment(
    int id,
    [FromBody] UpdateTicketAssignmentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            return BadRequest("Assigned user is required.");
        }

        var ticket = await _db.OrderTickets
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return BadRequest("Closed tickets cannot be reassigned.");
        }

        var hasStoreAccess = await _storeAccessService.HasAccessAsync(
    userId,
    ticket.StoreId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        var assigneeHasStoreAccess = await _db.UserStoreAccesses
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == request.AssignedToUserId &&
                x.StoreId == ticket.StoreId);

        if (!assigneeHasStoreAccess)
        {
            return BadRequest(
                "Assigned user does not have access to this ticket's store.");
        }

        var assigneeRoles = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == request.AssignedToUserId
            select role.Name!)
            .ToListAsync();

        var assigneeCanReceive =
            assigneeRoles.Contains("Admin") ||
            assigneeRoles.Contains("CustomerSupport") ||
            assigneeRoles.Contains("WarehouseStaff");

        if (!assigneeCanReceive)
        {
            return BadRequest(
                "Selected user cannot receive order tickets.");
        }

        ticket.AssignedToUserId = request.AssignedToUserId;

        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> CreateTicketFromPage(
    [FromQuery] int storeId,
    [FromBody] CreateTicketFromPageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var canCreate =
            User.IsInRole("Admin") ||
            User.IsInRole("CustomerSupport") ||
            User.IsInRole("WarehouseStaff");

        if (!canCreate)
        {
            return Forbid();
        }

        var hasStoreAccess = await _storeAccessService.HasAccessAsync(userId, storeId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            return BadRequest("Assigned user is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var title = request.Title.Trim();
        var message = request.Message.Trim();

        if (title.Length > 200)
        {
            return BadRequest("Title cannot exceed 200 characters.");
        }

        if (message.Length > 4000)
        {
            return BadRequest("Message cannot exceed 4000 characters.");
        }

        var assigneeHasStoreAccess = await _db.UserStoreAccesses
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == request.AssignedToUserId &&
                x.StoreId == storeId);

        if (!assigneeHasStoreAccess)
        {
            return BadRequest(
                "Assigned user does not have access to this store.");
        }

        var assigneeRoles = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == request.AssignedToUserId
            select role.Name!)
            .ToListAsync();

        var assigneeCanReceive =
            assigneeRoles.Contains("Admin") ||
            assigneeRoles.Contains("CustomerSupport") ||
            assigneeRoles.Contains("WarehouseStaff");

        if (!assigneeCanReceive)
        {
            return BadRequest(
                "Selected user cannot receive order tickets.");
        }

        int? orderId = null;

        if (!string.IsNullOrWhiteSpace(request.DisplayOrderId))
        {
            var displayOrderId = request.DisplayOrderId.Trim();

            orderId = await _db.Orders
                .AsNoTracking()
                .Where(x =>
                    x.StoreId == storeId &&
                    x.DisplayOrderId == displayOrderId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (!orderId.HasValue)
            {
                return BadRequest(
                    "Order ID was not found in the selected store.");
            }
        }

        var ticket = new OrderTicket
        {
            StoreId = storeId,
            OrderId = orderId,
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = userId,
            Title = title,
            Message = message,
            Status = TicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.OrderTickets.Add(ticket);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ticket.Id
        });
    }
}