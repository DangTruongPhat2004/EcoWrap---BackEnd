using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IOrderService
    {
        Task<object> CreateOrderFromActiveCartAsync(int userId);
    }
}