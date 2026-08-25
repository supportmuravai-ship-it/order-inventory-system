using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Stores;
using OrderManagement.Core.Entities;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Core.Interfaces;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStoreAccessService _storeAccessService;

    public StoresController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStoreAccessService storeAccessService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _storeAccessService = storeAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StoreDto>>> GetStores()
    {
        var userId = _userManager.GetUserId(User); // 'User' comes from ControllerBase. It represents the currently authenticated request user as a ClaimsPrincipal.

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var stores = await _dbContext.UserStoreAccesses
            .Where(x =>
                x.UserId == userId &&
                x.Store.IsActive)
            .OrderBy(x => x.Store.Name)
            .Select(x => new StoreDto
            {
                Id = x.Store.Id,
                Name = x.Store.Name,
                Code = x.Store.Code
            })
            .ToListAsync();

        return Ok(stores);
    }

    [HttpGet("{storeId:int}/access-test")]
    public async Task<IActionResult> AccessTest(int storeId)
    {
        var userId = _userManager.GetUserId(User);

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

        return Ok(new
        {
            message = "You have access to this store.",
            storeId
        });
    }
}