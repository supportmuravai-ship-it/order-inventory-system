using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Common;
using OrderManagement.Core.DTOs.Tickets;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;

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

    // =========================================================
    // GET TICKETS
    // =========================================================

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

        // Non-admin users can see:
        // 1. Tickets assigned to them
        // 2. Tickets created by them
        if (!isAdmin)
        {
            query = query.Where(x =>
                x.Assignees.Any(a => a.UserId == userId) ||
                x.CreatedByUserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            // Admin filter still works exactly like before:
            // show all tickets containing this user as an assignee.
            query = query.Where(x =>
                x.Assignees.Any(a =>
                    a.UserId == request.AssignedToUserId));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == request.Status.Value);
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

        /*
         * We first load the tickets with their assignee IDs.
         *
         * We are keeping TicketListItemDto backward-compatible for now:
         *
         * AssignedToUserId = first assigned user's ID
         * AssignedToEmail  = all assigned emails joined with commas
         *
         * This means your existing Angular page does not immediately break.
         */
        var rawItems = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,

                x.OrderId,

                DisplayOrderId =
                    x.Order != null
                        ? x.Order.DisplayOrderId
                        : null,

                AssignedUserIds = x.Assignees
                    .Select(a => a.UserId)
                    .ToList(),

                x.CreatedByUserId,

                CreatedByEmail = _db.Users
                    .Where(user =>
                        user.Id == x.CreatedByUserId)
                    .Select(user => user.Email ?? "")
                    .FirstOrDefault(),

                x.Title,
                x.Status,
                x.CreatedAtUtc,
                x.ClosedAtUtc
            })
            .ToListAsync();

        var allAssigneeIds = rawItems
            .SelectMany(x => x.AssignedUserIds)
            .Distinct()
            .ToList();

        var assigneeEmails = await _db.Users
            .AsNoTracking()
            .Where(x => allAssigneeIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Email = x.Email ?? ""
            })
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Email);

        var items = rawItems
            .Select(x =>
            {
                var orderedAssigneeIds = x.AssignedUserIds
                    .OrderBy(id => id)
                    .ToList();

                var emails = orderedAssigneeIds
                    .Where(id => assigneeEmails.ContainsKey(id))
                    .Select(id => assigneeEmails[id])
                    .Where(email =>
                        !string.IsNullOrWhiteSpace(email))
                    .ToList();

                return new TicketListItemDto
                {
                    Id = x.Id,

                    OrderId = x.OrderId,
                    DisplayOrderId = x.DisplayOrderId,

                    // Backward compatibility
                    AssignedToUserId =
                        orderedAssigneeIds.FirstOrDefault() ?? "",

                    // Shows ALL assignees
                    AssignedToEmail =
                        string.Join(", ", emails),

                    CreatedByUserId = x.CreatedByUserId,
                    CreatedByEmail =
                        x.CreatedByEmail ?? "",

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
                };
            })
            .ToList();

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

    // =========================================================
    // MY OPEN TICKET COUNT
    // =========================================================

    [HttpGet("my-open-count")]
    public async Task<ActionResult<int>> GetMyOpenCount(
        [FromQuery] int storeId)
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

        var count = await _db.OrderTickets
            .AsNoTracking()
            .CountAsync(x =>
                x.StoreId == storeId &&
                x.Assignees.Any(a =>
                    a.UserId == userId) &&
                x.Status == TicketStatus.Open);

        return Ok(count);
    }

    // =========================================================
    // ASSIGNABLE USERS
    // =========================================================

    [HttpGet("assignable-users")]
    public async Task<ActionResult<List<AssignableTicketUserDto>>>
        GetAssignableUsers(
            [FromQuery] int storeId)
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

        var users = await (
            from access in _db.UserStoreAccesses
            join user in _db.Users
                on access.UserId equals user.Id
            where access.StoreId == storeId
            orderby user.Email
            select new
            {
                user.Id,
                Email = user.Email ?? ""
            })
            .AsNoTracking()
            .ToListAsync();

        var result =
            new List<AssignableTicketUserDto>();

        foreach (var user in users)
        {
            var roles = await (
                from userRole in _db.UserRoles
                join role in _db.Roles
                    on userRole.RoleId equals role.Id
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

            result.Add(
                new AssignableTicketUserDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
        }

        return Ok(result);
    }

    // =========================================================
    // CREATE TICKET FROM ORDER
    // =========================================================

    [HttpPost("/api/orders/{orderId:int}/tickets")]
    public async Task<ActionResult> CreateTicket(
        int orderId,
        [FromBody] CreateTicketRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

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

        var assignedUserIds =
            NormalizeAssignedUserIds(
                request.AssignedToUserIds);

        if (assignedUserIds.Count == 0)
        {
            return BadRequest(
                "At least one assigned user is required.");
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
            return BadRequest(
                "Title cannot exceed 200 characters.");
        }

        if (message.Length > 4000)
        {
            return BadRequest(
                "Message cannot exceed 4000 characters.");
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

        var hasStoreAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                order.StoreId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        var assigneeValidationError =
            await ValidateAssigneesAsync(
                assignedUserIds,
                order.StoreId);

        if (assigneeValidationError is not null)
        {
            return BadRequest(
                assigneeValidationError);
        }

        var ticket = new OrderTicket
        {
            StoreId = order.StoreId,
            OrderId = order.Id,

            CreatedByUserId = userId,

            Title = title,
            Message = message,

            Status = TicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,

            Assignees = assignedUserIds
                .Select(id =>
                    new OrderTicketAssignee
                    {
                        UserId = id
                    })
                .ToList()
        };

        _db.OrderTickets.Add(ticket);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ticket.Id
        });
    }

    // =========================================================
    // GET SINGLE TICKET
    // =========================================================

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailsDto>>
        GetTicket(int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ticket = await _db.OrderTickets
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Assignees)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                ticket.StoreId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin &&
            !ticket.Assignees.Any(
                a => a.UserId == userId) &&
            ticket.CreatedByUserId != userId)
        {
            return Forbid();
        }

        var assignedUserIds = ticket.Assignees
            .Select(x => x.UserId)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var assignedEmails = await _db.Users
            .AsNoTracking()
            .Where(x =>
                assignedUserIds.Contains(x.Id))
            .Select(x => x.Email ?? "")
            .Where(x => x != "")
            .ToListAsync();

        var createdByEmail = await _db.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == ticket.CreatedByUserId)
            .Select(x => x.Email ?? "")
            .FirstOrDefaultAsync() ?? "";

        var result = new TicketDetailsDto
        {
            Id = ticket.Id,

            OrderId = ticket.OrderId,

            DisplayOrderId =
                ticket.Order != null
                    ? ticket.Order.DisplayOrderId
                    : null,

            /*
             * Backward compatibility:
             * keep existing DTO fields for now.
             */
            AssignedToUserId =
                assignedUserIds.FirstOrDefault() ?? "",

            AssignedToEmail =
                string.Join(", ", assignedEmails),

            CreatedByUserId =
                ticket.CreatedByUserId,

            CreatedByEmail =
                createdByEmail,

            ClosedByUserId =
                ticket.ClosedByUserId,

            Title = ticket.Title,
            Message = ticket.Message,

            Status = ticket.Status,

            CreatedAtUtc = DateTime.SpecifyKind(
                ticket.CreatedAtUtc,
                DateTimeKind.Utc),

            ClosedAtUtc =
                ticket.ClosedAtUtc.HasValue
                    ? DateTime.SpecifyKind(
                        ticket.ClosedAtUtc.Value,
                        DateTimeKind.Utc)
                    : null
        };

        if (!string.IsNullOrWhiteSpace(
                ticket.ClosedByUserId))
        {
            result.ClosedByEmail =
                await _db.Users
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == ticket.ClosedByUserId)
                    .Select(x =>
                        x.Email ?? "")
                    .FirstOrDefaultAsync();
        }

        return Ok(result);
    }

    // =========================================================
    // CLOSE TICKET
    // =========================================================

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult> CloseTicket(
        int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ticket = await _db.OrderTickets
            .Include(x => x.Order)
            .Include(x => x.Assignees)
            .FirstOrDefaultAsync(
                x => x.Id == id);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        var hasAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                ticket.StoreId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole("Admin");

        /*
         * Same permission rule as before,
         * except ANY assignee can now close it.
         */
        if (!isAdmin &&
            !ticket.Assignees.Any(
                x => x.UserId == userId) &&
            ticket.CreatedByUserId != userId)
        {
            return Forbid();
        }

        if (ticket.Status ==
            TicketStatus.Closed)
        {
            return BadRequest(
                "Ticket is already closed.");
        }

        ticket.Status =
            TicketStatus.Closed;

        ticket.ClosedAtUtc =
            DateTime.UtcNow;

        ticket.ClosedByUserId =
            userId;

        await _db.SaveChangesAsync();

        return Ok();
    }

    // =========================================================
    // UPDATE / REASSIGN TICKET
    // =========================================================

    [HttpPatch("{id:int}/assignment")]
    public async Task<ActionResult> UpdateAssignment(
        int id,
        [FromBody] UpdateTicketAssignmentRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var assignedUserIds =
            NormalizeAssignedUserIds(
                request.AssignedToUserIds);

        if (assignedUserIds.Count == 0)
        {
            return BadRequest(
                "At least one assigned user is required.");
        }

        var ticket = await _db.OrderTickets
            .Include(x => x.Order)
            .Include(x => x.Assignees)
            .FirstOrDefaultAsync(
                x => x.Id == id);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (ticket.Status ==
            TicketStatus.Closed)
        {
            return BadRequest(
                "Closed tickets cannot be reassigned.");
        }

        var hasStoreAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                ticket.StoreId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        var assigneeValidationError =
            await ValidateAssigneesAsync(
                assignedUserIds,
                ticket.StoreId);

        if (assigneeValidationError is not null)
        {
            return BadRequest(
                assigneeValidationError);
        }

        var existingIds = ticket.Assignees
            .Select(x => x.UserId)
            .ToHashSet();

        /*
         * Remove users who are no longer selected.
         */
        var assigneesToRemove =
            ticket.Assignees
                .Where(x =>
                    !assignedUserIds.Contains(
                        x.UserId))
                .ToList();

        _db.OrderTicketAssignees
            .RemoveRange(assigneesToRemove);

        /*
         * Add newly selected users.
         */
        var assigneesToAdd =
            assignedUserIds
                .Where(id =>
                    !existingIds.Contains(id))
                .Select(id =>
                    new OrderTicketAssignee
                    {
                        OrderTicketId = ticket.Id,
                        UserId = id
                    })
                .ToList();

        if (assigneesToAdd.Count > 0)
        {
            _db.OrderTicketAssignees
                .AddRange(assigneesToAdd);
        }

        await _db.SaveChangesAsync();

        return Ok();
    }

    // =========================================================
    // CREATE TICKET FROM TICKETS PAGE
    // =========================================================

    [HttpPost]
    public async Task<ActionResult>
        CreateTicketFromPage(
            [FromQuery] int storeId,
            [FromBody]
            CreateTicketFromPageRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

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

        var hasStoreAccess =
            await _storeAccessService.HasAccessAsync(
                userId,
                storeId);

        if (!hasStoreAccess)
        {
            return Forbid();
        }

        var assignedUserIds =
            NormalizeAssignedUserIds(
                request.AssignedToUserIds);

        if (assignedUserIds.Count == 0)
        {
            return BadRequest(
                "At least one assigned user is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Title))
        {
            return BadRequest(
                "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Message))
        {
            return BadRequest(
                "Message is required.");
        }

        var title =
            request.Title.Trim();

        var message =
            request.Message.Trim();

        if (title.Length > 200)
        {
            return BadRequest(
                "Title cannot exceed 200 characters.");
        }

        if (message.Length > 4000)
        {
            return BadRequest(
                "Message cannot exceed 4000 characters.");
        }

        var assigneeValidationError =
            await ValidateAssigneesAsync(
                assignedUserIds,
                storeId);

        if (assigneeValidationError is not null)
        {
            return BadRequest(
                assigneeValidationError);
        }

        int? orderId = null;

        if (!string.IsNullOrWhiteSpace(
                request.DisplayOrderId))
        {
            var displayOrderId =
                request.DisplayOrderId.Trim();

            orderId = await _db.Orders
                .AsNoTracking()
                .Where(x =>
                    x.StoreId == storeId &&
                    x.DisplayOrderId ==
                        displayOrderId)
                .Select(x =>
                    (int?)x.Id)
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

            CreatedByUserId =
                userId,

            Title = title,
            Message = message,

            Status =
                TicketStatus.Open,

            CreatedAtUtc =
                DateTime.UtcNow,

            Assignees =
                assignedUserIds
                    .Select(id =>
                        new OrderTicketAssignee
                        {
                            UserId = id
                        })
                    .ToList()
        };

        _db.OrderTickets.Add(ticket);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ticket.Id
        });
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<ActionResult> ReopenTicket(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ticket = await _db.OrderTickets
            .Include(x => x.Assignees)
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
            !ticket.Assignees.Any(x => x.UserId == userId) &&
            ticket.CreatedByUserId != userId)
        {
            return Forbid();
        }

        if (ticket.Status == TicketStatus.Open)
        {
            return BadRequest("Ticket is already open.");
        }

        ticket.Status = TicketStatus.Open;
        ticket.ClosedAtUtc = null;
        ticket.ClosedByUserId = null;

        await _db.SaveChangesAsync();

        return Ok();
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    private static List<string>
        NormalizeAssignedUserIds(
            IEnumerable<string>? userIds)
    {
        if (userIds is null)
        {
            return [];
        }

        return userIds
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();
    }

    private async Task<string?>
        ValidateAssigneesAsync(
            List<string> assignedUserIds,
            int storeId)
    {
        /*
         * Make sure EVERY selected user
         * has access to this store.
         */
        var usersWithStoreAccess =
            await _db.UserStoreAccesses
                .AsNoTracking()
                .Where(x =>
                    x.StoreId == storeId &&
                    assignedUserIds.Contains(
                        x.UserId))
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

        if (usersWithStoreAccess.Count !=
            assignedUserIds.Count)
        {
            return
                "One or more selected users do not have access to this store.";
        }

        /*
         * Same roles that were allowed before:
         *
         * Admin
         * CustomerSupport
         * WarehouseStaff
         */
        string[] allowedRoles =
        [
            "Admin",
            "CustomerSupport",
            "WarehouseStaff"
        ];

        var usersWithAllowedRole =
            await (
                from userRole in _db.UserRoles
                join role in _db.Roles
                    on userRole.RoleId
                    equals role.Id

                where
                    assignedUserIds.Contains(
                        userRole.UserId) &&
                    role.Name != null &&
                    allowedRoles.Contains(
                        role.Name)

                select userRole.UserId
            )
            .Distinct()
            .ToListAsync();

        if (usersWithAllowedRole.Count !=
            assignedUserIds.Count)
        {
            return
                "One or more selected users cannot receive order tickets.";
        }

        return null;
    }
}