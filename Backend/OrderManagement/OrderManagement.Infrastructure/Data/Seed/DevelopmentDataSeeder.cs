using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Entities;

namespace OrderManagement.Infrastructure.Data.Seed;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var roles = new[]
        {
            "Admin",
            "InventoryManager",
            "CustomerSupport",
            "WarehouseStaff"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        if (!await dbContext.Stores.AnyAsync())
        {
            dbContext.Stores.AddRange(
                new Store
                {
                    Name = "UAE Store",
                    Code = "UAE",
                    IsActive = true
                },
                new Store
                {
                    Name = "Test Store",
                    Code = "TEST",
                    IsActive = true
                }
            );

            await dbContext.SaveChangesAsync();
        }

        var admin = await CreateUserAsync(
            userManager,
            name: "Development Admin",
            email: "admin@local.test",
            password: "Admin123!",
            role: "Admin");

        var inventoryManager = await CreateUserAsync(
            userManager,
            name: "Development Inventory Manager",
            email: "inventory@local.test",
            password: "Inventory123!",
            role: "InventoryManager");

        var customerSupport = await CreateUserAsync(
            userManager,
            name: "Development Customer Support",
            email: "support@local.test",
            password: "Support123!",
            role: "CustomerSupport");

        var warehouseStaff = await CreateUserAsync(
            userManager,
            name: "Development Warehouse Staff",
            email: "warehouse@local.test",
            password: "Warehouse123!",
            role: "WarehouseStaff");

        var stores = await dbContext.Stores
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (stores.Count < 2)
        {
            return;
        }

        var uaeStore = stores[0];
        var testStore = stores[1];

        await AddStoreAccessAsync(dbContext, admin.Id, uaeStore.Id);
        await AddStoreAccessAsync(dbContext, admin.Id, testStore.Id);

        await AddStoreAccessAsync(dbContext, inventoryManager.Id, uaeStore.Id);

        await AddStoreAccessAsync(dbContext, customerSupport.Id, uaeStore.Id);

        await AddStoreAccessAsync(dbContext, warehouseStaff.Id, testStore.Id);

        await dbContext.SaveChangesAsync();

        await Phase2OrderDataSeeder.SeedAsync(dbContext);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string name,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Name = name,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to create development user '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task AddStoreAccessAsync(
        AppDbContext dbContext,
        string userId,
        int storeId)
    {
        var exists = await dbContext.UserStoreAccesses
            .AnyAsync(x =>
                x.UserId == userId &&
                x.StoreId == storeId);

        if (!exists)
        {
            dbContext.UserStoreAccesses.Add(
                new UserStoreAccess
                {
                    UserId = userId,
                    StoreId = storeId
                });
        }
    }
}