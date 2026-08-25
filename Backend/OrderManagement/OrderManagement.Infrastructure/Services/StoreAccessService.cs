using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Services;

public class StoreAccessService : IStoreAccessService
{
    private readonly AppDbContext _dbContext;

    public StoreAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasAccessAsync(string userId, int storeId)
    {
        return await _dbContext.UserStoreAccesses
            .AnyAsync(x =>
                x.UserId == userId &&
                x.StoreId == storeId &&
                x.Store.IsActive);
    }
}