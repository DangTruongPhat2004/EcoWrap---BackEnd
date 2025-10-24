using EcoWrap.Models.Request;
using EcoWrap.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IWarehouseService
    {
        Task<IEnumerable<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(int id);
        Task<bool> CreateWarehouseAsync(int userId, CreateWarehouseRequest request);
        Task<bool> UpdateWarehouseAsync(int warehouseId, int userId, UpdateWarehouseRequest request);
        Task<IEnumerable<Warehouse>> GetWarehousesBySellerAsync(int userId);

        // [SỬA ĐỔI] Cập nhật chữ ký (signature) của hàm DeleteAsync
        Task<bool> DeleteAsync(int warehouseId, int userId);
    }
}