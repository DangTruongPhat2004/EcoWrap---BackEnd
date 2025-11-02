using EcoWrap.BLL.IServices;
using EcoWrap.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            try
            {
                var result = await _dashboardService.GetDashboardMetricsAsync();

                if (result.Success)
                {
                    return Ok(result);
                }

                // Nếu service bắt lỗi, trả về 500
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (Exception ex)
            {
                // Bắt các lỗi chưa xử lý
                Console.WriteLine($"[API GetDashboardMetrics EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống." });
            }
        }
    }
}