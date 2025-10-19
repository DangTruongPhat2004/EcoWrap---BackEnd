using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface ICartService
    {
        Task<object> CreateCartAsync(int userId);
        Task<object> GetAllCartsAsync(int userId);
        Task<object> GetCartByIdAsync(int userId, int cartId);
        Task<object> DeleteCartAsync(int userId, int cartId);
    }
}