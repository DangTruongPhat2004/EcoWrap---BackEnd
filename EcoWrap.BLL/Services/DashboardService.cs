using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Response;

namespace EcoWrap.BLL.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepo;
        private readonly IFeedbackRepository _feedbackRepo;
        private readonly IOrderRepository _orderRepo;

        public DashboardService(
            IUserRepository userRepo,
            IFeedbackRepository feedbackRepo,
            IOrderRepository orderRepo)
        {
            _userRepo = userRepo;
            _feedbackRepo = feedbackRepo;
            _orderRepo = orderRepo;
        }

        public async Task<ApiResponse<DashboardResponse>> GetDashboardMetricsAsync()
        {
            try
            {
                // 1. Lấy dữ liệu User
                var userMetrics = await _userRepo.GetUserMetricsAsync(); // Await ngay lập tức

                // 2. Lấy dữ liệu Feedback
                var feedbackMetrics = await _feedbackRepo.GetFeedbackMetricsAsync(); // Await ngay lập tức

                // 3. Lấy dữ liệu Order
                var orderMetrics = await _orderRepo.GetCompletedOrderMetricsAsync(); // Await ngay lập tức

                // 4. Tổng hợp thành Response DTO
                var responseData = new DashboardResponse
                {
                    TotalVerifiedUsers = userMetrics.TotalVerified,
                    TotalCustomer = userMetrics.TotalCustomers,
                    TotalSeller = userMetrics.TotalSellers,
                    TotalFeedbacks = feedbackMetrics.Count,
                    AverageRating = Math.Round(feedbackMetrics.AverageRating, 2),
                    TotalRevenue = orderMetrics.TotalRevenue,
                    TotalSuccessfulTransactions = orderMetrics.Count
                };

                return new ApiResponse<DashboardResponse>
                {
                    Success = true,
                    Data = responseData
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardService.GetDashboardMetricsAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<DashboardResponse> { Success = false, Message = "Lỗi hệ thống khi tải dữ liệu dashboard." };
            }
        }
    }
}