using EcoWrap.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface ICartRepository
    {
        // Lấy tất cả carts
        Task<IEnumerable<Cart>> GetAllAsync();

        // Lấy cart theo ID
        Task<Cart?> GetByIdAsync(int id);

        // Lấy cart đang hoạt động (IsActive = true) của 1 customer
        Task<Cart?> GetActiveCartByCustomerIdAsync(int customerId);

        // Lấy tất cả carts (kể cả inactive) của 1 customer
        Task<IEnumerable<Cart>> GetByCustomerIdAsync(int customerId);

        // Thêm cart mới
        Task AddAsync(Cart entity);

        // Cập nhật cart
        Task UpdateAsync(Cart entity);

        // Xóa cart
        Task DeleteAsync(Cart entity);

        // Lưu thay đổi
        Task SaveChangesAsync();
    }
}