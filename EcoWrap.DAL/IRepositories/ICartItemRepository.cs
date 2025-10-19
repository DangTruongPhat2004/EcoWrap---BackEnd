using EcoWrap.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface ICartItemRepository
    {
        // Lấy cart item theo ID
        Task<CartItem?> GetByIdAsync(int id);

        // Lấy cart item theo cartId và productId
        Task<CartItem?> GetByCartAndProductAsync(int cartId, string productId);

        // Lấy toàn bộ items trong một cart
        Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId);

        // Thêm mới
        Task AddAsync(CartItem entity);

        // Cập nhật
        Task UpdateAsync(CartItem entity);

        // Xóa 1 item
        Task DeleteAsync(CartItem entity);

        // Xóa nhiều item cùng lúc (tối ưu khi xoá giỏ hàng)
        Task DeleteRangeAsync(IEnumerable<CartItem> entities);

        // Lưu thay đổi
        Task SaveChangesAsync();
    }
}