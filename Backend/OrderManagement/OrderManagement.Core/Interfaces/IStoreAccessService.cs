namespace OrderManagement.Core.Interfaces;

public interface IStoreAccessService
{
    Task<bool> HasAccessAsync(string userId, int storeId);
}