using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.IServices
{
    public interface IInventoryService
    {
        Task<Inventory> CreateInventoryAsync(int userId, CreatInventoryRequest request);
        Task<IEnumerable<Inventory>> GetAllAsync();
        Task<Inventory?> GetByIdAsync(int inventoryId);
        Task<IEnumerable<Inventory>> GetMyInventoryAsync(int userId);
        Task<Inventory> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request);
        Task<bool> DeleteInventoryAsync(int inventoryId);
    }
}