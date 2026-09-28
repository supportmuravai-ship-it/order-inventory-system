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
        var email = Environment.GetEnvironmentVariable("SETUP_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("SETUP_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new
            {
                error = "SETUP_ADMIN_EMAIL or SETUP_ADMIN_PASSWORD is missing."
            });
        }

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                error = "Admin user already exists."
            });
        }

        var user = new ApplicationUser
        {
            Name = "Production Admin",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        return Ok(new
        {
            message = "Admin created successfully",
            email
        });
    }
}