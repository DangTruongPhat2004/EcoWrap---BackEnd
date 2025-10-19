using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using System;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder()
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });

                int userId = int.Parse(userIdClaim);

                //Gọi service để tự động tạo đơn hàng từ giỏ hàng đang hoạt động
                var result = await _orderService.CreateOrderFromActiveCartAsync(userId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi nghiệp vụ (VD: không có giỏ hàng, giỏ hàng trống, v.v.)
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Token không hợp lệ hoặc không khớp
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Lỗi hệ thống
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tạo đơn hàng.", Detail = ex.Message });
            }
        }
    }
}