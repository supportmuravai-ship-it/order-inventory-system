using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Core.Entities;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/setup")]
public class ProductionSetupController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ProductionSetupController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin()
    {
        try
        {
            var email = Environment.GetEnvironmentVariable("SETUP_ADMIN_EMAIL");
            var password = Environment.GetEnvironmentVariable("SETUP_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new
                {
                    error = "Admin setup variables are missing."
                });
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole("Admin"));

                if (!roleResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Failed to create Admin role.",
                        details = roleResult.Errors
                    });
                }
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    Name = "Production Admin",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(
                    user,
                    password);

                if (!createResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Failed to create user.",
                        details = createResult.Errors
                    });
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Admin");

                if (!roleResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Failed to assign Admin role.",
                        details = roleResult.Errors
                    });
                }
            }

            return Ok(new
            {
                message = "Admin created successfully",
                email = user.Email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }
}