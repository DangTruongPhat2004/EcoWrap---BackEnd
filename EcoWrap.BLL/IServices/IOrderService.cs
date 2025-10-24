using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using EcoWrap.Models.Responses;
using System.Threading.Tasks;
using System.Collections.Generic; // Thêm cho GetAllOrderAsync

namespace EcoWrap.BLL.IServices
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderResponse>> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<ApiResponse<bool>> ManuallyConfirmPaymentAndPackageAsync(int orderId);
        Task<ApiResponse<GhtkResponse>> ConfirmShipmentByAdminAsync(int orderId);
        Task<ApiResponse<bool>> ConfirmDeliveryAsync(int orderId, int userId);
        Task AutoCancelOrderAsync(int orderId);
        Task<ApiResponse<bool>> DeclineOrderAsync(int orderId);
        //Task<ApiResponse<List<OrderResponse>>> GetAllOrderAsync(int userId);
        Task<ApiResponse<OrderDetailResponse>> GetOrderByIdAsync(int orderId, int userId);
    }
}