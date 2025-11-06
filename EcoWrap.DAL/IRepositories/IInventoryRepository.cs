using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IInventoryRepository
    {
        Task AddAsync(Inventory inventory);
        Task<Inventory?> GetByIdAsync(int inventoryId);
        Task<IEnumerable<Inventory>> GetAllAsync();
        Task UpdateAsync(Inventory inventory);
        Task DeleteAsync(Inventory inventory);
        Task SaveChangesAsync();
        Task<Inventory?> GetByProductAndWarehouseAsync(string productId, int warehouseId);
        Task<bool> DecreaseStockAcrossWarehousesAsync(string productId, int quantityToDecrease);
        Task IncreaseStockInFirstWarehouseAsync(string productId, int quantityToIncrease);
        Task<IEnumerable<Inventory>> GetInventoriesByProductIdsAsync(IEnumerable<string> productIds);
    }
}