using EcoWrap.Models.Request;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface ICartItemService
    {
        Task<object> CreateAsync(CreateCartItemRequest request, int userId);
        Task<object> UpdateAsync(int cartItemId, UpdateCartItemRequest request, int userId);
        Task<bool> DeleteAsync(int cartItemId, int userId);
    }
}