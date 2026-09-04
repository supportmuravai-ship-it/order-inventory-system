using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Admin;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
namespace OrderManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly ShopifyReconciliationService _shopifyReconciliationService;
    public AdminController(
     AppDbContext dbContext,
     UserManager<ApplicationUser> userManager,
     RoleManager<IdentityRole> roleManager,
     ShopifyReconciliationService shopifyReconciliationService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _shopifyReconciliationService = shopifyReconciliationService;
    }

    [HttpGet("order-kpis")]
    public async Task<ActionResult<AdminOrderKpiDto>> GetOrderKpis()
    {
        var attentionThreshold = DateTime.UtcNow.AddHours(-12);

        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Store.IsActive);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new AdminOrderKpiDto
            {
                TotalOrders = group.Count(),

                New = group.Count(x =>
                    x.OrderStatus == OrderStatus.New),

                Confirmed = group.Count(x =>
                    x.OrderStatus == OrderStatus.Confirmed),

                Shipped = group.Count(x =>
                    x.OrderStatus == OrderStatus.Shipped),

                Delivered = group.Count(x =>
                    x.OrderStatus == OrderStatus.Delivered),

                Cancelled = group.Count(x =>
                    x.OrderStatus == OrderStatus.Cancelled),

                NoResponse = group.Count(x =>
                    x.OrderStatus == OrderStatus.NoResponse),

                Return = group.Count(x =>
                    x.OrderStatus == OrderStatus.Return),

                ReturnInProcess = group.Count(x =>
                    x.OrderStatus == OrderStatus.ReturnInProcess),

                RepeatedOrder = group.Count(x =>
                    x.OrderStatus == OrderStatus.RepeatedOrder),

                Returns = group.Count(x =>
                    x.OrderStatus == OrderStatus.Return ||
                    x.OrderStatus == OrderStatus.ReturnInProcess),

                NeedsAttention = group.Count(x =>
                    x.OrderStatus != OrderStatus.Delivered &&
                    x.OrderStatus != OrderStatus.Return &&
                    x.OrderStatus != OrderStatus.Cancelled &&
                    x.OrderStatus != OrderStatus.RepeatedOrder &&
                    x.LastStatusChangedAtUtc <= attentionThreshold),

                NeedToShip = group.Count(x =>
                    x.NeedToShip)
            })
            .SingleOrDefaultAsync();

        return Ok(summary ?? new AdminOrderKpiDto());
    }

    [HttpGet("shopify-health")]
    public async Task<ActionResult<List<AdminShopifyHealthDto>>> GetShopifyHealth()
    {
        var stores = await _dbContext.Stores
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AdminShopifyHealthDto
            {
                StoreId = x.Id,
                StoreName = x.Name,
                StoreCode = x.Code,
                IsActive = x.IsActive,
                ShopDomain = x.ShopDomain,

                ConnectionStatus =
                    x.ShopDomain != null &&
                    x.ShopDomain != "" &&
                    x.ShopifyAccessTokenEncrypted != null &&
                    x.ShopifyAccessTokenEncrypted != "" &&
                    x.ShopifyConnectedAtUtc != null
                        ? "Connected"
                        : "Not Connected",

                ShopifyConnectedAtUtc = x.ShopifyConnectedAtUtc,

                LastSuccessfulSyncAtUtc =
                    x.LastSuccessfulSyncAtUtc,

                LastReconciliationAtUtc =
                    x.LastReconciliationAtUtc,

                LastWebhookReceivedAtUtc =
                    x.LastWebhookReceivedAtUtc,

                LastShopifyError =
                    x.LastShopifyError
            })
            .ToListAsync();

        foreach (var store in stores)
        {
            store.SetUtcKinds();
        }

        return Ok(stores);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
    CreateAdminUserRequest request,
    CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var role = request.Role.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Password is required.");
        }

        var allowedRoles = new[]
        {
        "Admin",
        "InventoryManager",
        "CustomerSupport",
        "WarehouseStaff"
    };

        if (!allowedRoles.Contains(role))
        {
            return BadRequest("Invalid role.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest($"Role '{role}' does not exist.");
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return Conflict("A user with this email already exists.");
        }

        var storeIds = request.StoreIds
            .Distinct()
            .ToList();

        if (storeIds.Count == 0)
        {
            return BadRequest("At least one store must be assigned.");
        }

        var validStoreIds = await _dbContext.Stores
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                storeIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validStoreIds.Count != storeIds.Count)
        {
            return BadRequest("One or more selected stores are invalid or inactive.");
        }

        var now = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            Name = name,
            Email = email,
            UserName = email,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var createResult =
            await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return BadRequest(new
            {
                message = "Could not create user.",
                errors = createResult.Errors.Select(x => x.Description)
            });
        }

        var roleResult =
            await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return BadRequest(new
            {
                message = "Could not assign role.",
                errors = roleResult.Errors.Select(x => x.Description)
            });
        }

        foreach (var storeId in storeIds)
        {
            _dbContext.UserStoreAccesses.Add(new UserStoreAccess
            {
                UserId = user.Id,
                StoreId = storeId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            role,
            storeIds
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserListItemDto>>> GetUsers()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                Email = x.Email ?? string.Empty,
                x.IsActive,

                Roles = _dbContext.UserRoles
                    .Where(ur => ur.UserId == x.Id)
                    .Join(
                        _dbContext.Roles,
                        ur => ur.RoleId,
                        role => role.Id,
                        (ur, role) => role.Name!)
                    .ToList(),

                StoreIds = x.StoreAccesses
                .Select(a => a.StoreId)
                .ToList(),

                Stores = x.StoreAccesses
                    .OrderBy(a => a.Store.Name)
                    .Select(a => a.Store.Name)
                    .ToList()
            })
            .ToListAsync();

        var result = users
            .Select(x => new AdminUserListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                IsActive = x.IsActive,
                Roles = x.Roles,
                Stores = x.Stores,
                StoreIds = x.StoreIds,
            })
            .ToList();

        return Ok(result);
    }

    [HttpPut("users/{userId}/active")]
    public async Task<IActionResult> UpdateUserActiveStatus(
    string userId,
    [FromBody] bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Could not update user.",
                errors = result.Errors.Select(x => x.Description)
            });
        }

        return NoContent();
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(
    string userId,
    UpdateAdminUserRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return NotFound();
        }

        var role = request.Role.Trim();

        var allowedRoles = new[]
        {
        "Admin",
        "InventoryManager",
        "CustomerSupport",
        "WarehouseStaff"
    };

        if (!allowedRoles.Contains(role))
        {
            return BadRequest("Invalid role.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest($"Role '{role}' does not exist.");
        }

        var existingRoles =
            await _userManager.GetRolesAsync(user);

        if (existingRoles.Count == 1 &&
            existingRoles[0] == role)
        {
            return NoContent();
        }

        if (existingRoles.Count > 0)
        {
            var removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    existingRoles);

            if (!removeResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Could not remove existing role.",
                    errors = removeResult.Errors
                        .Select(x => x.Description)
                });
            }
        }

        var addResult =
            await _userManager.AddToRoleAsync(user, role);

        if (!addResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Could not assign role.",
                errors = addResult.Errors
                    .Select(x => x.Description)
            });
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpPut("users/{userId}/stores")]
    public async Task<IActionResult> UpdateUserStores(
    string userId,
    UpdateAdminUserStoresRequest request,
    CancellationToken cancellationToken)
    {
        var userExists =
            await _dbContext.Users
                .AnyAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (!userExists)
        {
            return NotFound();
        }

        var storeIds = request.StoreIds
            .Distinct()
            .ToList();

        if (storeIds.Count == 0)
        {
            return BadRequest(
                "At least one store must be assigned.");
        }

        var validStoreIds =
            await _dbContext.Stores
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    storeIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

        if (validStoreIds.Count != storeIds.Count)
        {
            return BadRequest(
                "One or more selected stores are invalid or inactive.");
        }

        var existingAccess =
            await _dbContext.UserStoreAccesses
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

        _dbContext.UserStoreAccesses
            .RemoveRange(existingAccess);

        foreach (var storeId in storeIds)
        {
            _dbContext.UserStoreAccesses.Add(
                new UserStoreAccess
                {
                    UserId = userId,
                    StoreId = storeId
                });
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return NoContent();
    }

    [HttpPost("stores")]
    public async Task<IActionResult> CreateStore(
    CreateAdminStoreRequest request,
    CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim();
        var shopDomain = request.ShopDomain.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Store Name is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Store Code is required.");
        }

        if (string.IsNullOrWhiteSpace(shopDomain))
        {
            return BadRequest("Shop Domain is required.");
        }

        if (!shopDomain.EndsWith(
                ".myshopify.com",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Shop Domain must end with .myshopify.com.");
        }

        var codeExists = await _dbContext.Stores
            .AnyAsync(
                x => x.Code == code,
                cancellationToken);

        if (codeExists)
        {
            return Conflict(
                "A store with this Store Code already exists.");
        }

        var domainExists = await _dbContext.Stores
            .AnyAsync(
                x => x.ShopDomain == shopDomain,
                cancellationToken);

        if (domainExists)
        {
            return Conflict(
                "A store with this Shopify domain already exists.");
        }

        var now = DateTime.UtcNow;

        var store = new Store
        {
            Name = name,
            Code = code,
            ShopDomain = shopDomain,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Stores.Add(store);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Ok(new
        {
            store.Id,
            store.Name,
            store.Code,
            store.ShopDomain,
            store.IsActive
        });
    }

    [HttpPost("stores/{storeId}/sync")]
    public async Task<IActionResult> SyncStore(
    int storeId,
    CancellationToken cancellationToken)
    {
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(
                x => x.Id == storeId &&
                     x.IsActive,
                cancellationToken);

        if (store == null)
        {
            return NotFound("Store not found.");
        }

        try
        {
            var updatedSinceUtc =
                store.LastReconciliationAtUtc ??
                DateTime.UtcNow.AddDays(-1);

            var result =
                await _shopifyReconciliationService.ReconcileStoreAsync(
                    storeId,
                    updatedSinceUtc,
                    cancellationToken);

            store.LastSuccessfulSyncAtUtc = DateTime.UtcNow;
            store.LastReconciliationAtUtc = DateTime.UtcNow;
            store.LastShopifyError = null;
            store.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return Ok(new
            {
                message = "Sync completed.",
                result
            });
        }
        catch (Exception ex)
        {
            store.LastShopifyError = ex.Message;
            store.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return BadRequest(new
            {
                message = "Sync failed.",
                error = ex.Message
            });
        }
    }
}