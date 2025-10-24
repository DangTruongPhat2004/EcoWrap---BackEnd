using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response; // Giả sử Ghtk models nằm ở đây

namespace EcoWrap.BLL.IServices
{
    public interface IGhtkService
    {
        Task<GhtkResponse> CreateShipmentAsync(Order order, User user);
        Task<bool> CancelShipmentAsync(string shippingCode);
    }
}