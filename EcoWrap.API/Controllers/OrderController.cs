using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using EcoWrap.Models.Responses; // Needed for ApiResponse
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all order actions
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST api/order
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ." });
                }

                var result = await _orderService.CreateOrderAsync(userId, request);

                if (result.Success)
                {
                    // CreatedAtAction sends a 201 Created response with a Location header
                    // pointing to the newly created resource (GetOrderById endpoint).
                    return CreatedAtAction(nameof(GetOrderById), new { orderId = result.Data.OrderId }, result);
                }
                else
                {
                    // Common reasons: Cart empty, Cart inactive
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API CreateOrder EXCEPTION]: {ex.ToString()}"); // Log full exception
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi tạo đơn hàng." });
            }
        }

        [HttpPost("{orderId}/confirm-packaging")]
        [Authorize(Roles = "Admin")] // Chỉ Admin được thực hiện
        public async Task<IActionResult> ManuallyConfirmAndPackage(int orderId)
        {
            try
            {
                var result = await _orderService.ManuallyConfirmPaymentAndPackageAsync(orderId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    // Kiểm tra các lỗi cụ thể từ service
                    if (result.Message.Contains("Không tìm thấy đơn hàng"))
                        return NotFound(result);
                    if (result.Message.Contains("không ở trạng thái 'Pending'"))
                        return BadRequest(result); // Trạng thái không hợp lệ
                    if (result.Message.Contains("không đủ tồn kho"))
                        return BadRequest(result); // Lỗi nghiệp vụ hết hàng

                    // Lỗi chung khác
                    return BadRequest(result);
                }
            }
            catch (Exception ex) // Bắt các lỗi không mong muốn khác
            {
                Console.WriteLine($"[API ManuallyConfirmAndPackage EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi xác nhận đơn hàng thủ công." });
            }
        }

        // POST api/order/{orderId}/confirm-shipment
        [HttpPost("{orderId}/confirm-shipment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmShipment(int orderId)
        {
            try
            {
                // Gọi đúng tên hàm mới trong Service
                var result = await _orderService.ConfirmShipmentByAdminAsync(orderId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    // Check common reasons for failure
                    if (result.Message.Contains("Không tìm thấy đơn hàng"))
                        return NotFound(result);
                    if (result.Message.Contains("không ở trạng thái 'Packaging'"))
                        return BadRequest(result); // Wrong state
                    if (result.Message.Contains("Lỗi khi tạo vận đơn GHTK"))
                        return StatusCode(502, result); // Bad Gateway - Error calling external service

                    return BadRequest(result); // General bad request
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ConfirmShipment EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi xác nhận giao hàng." });
            }
        }

        // POST api/order/{orderId}/confirm-delivery
        [HttpPost("{orderId}/confirm-delivery")]
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> ConfirmDelivery(int orderId)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ." });
                }

                var result = await _orderService.ConfirmDeliveryAsync(orderId, userId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    if (result.Message.Contains("Không tìm thấy đơn hàng"))
                        return NotFound(result);
                    if (result.Message.Contains("không có quyền xác nhận"))
                        return Forbid(); // Use 403 Forbidden if user doesn't own order
                    if (result.Message.Contains("không ở trạng thái 'Shipping'"))
                        return BadRequest(result); // Wrong state

                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ConfirmDelivery EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi xác nhận nhận hàng." });
            }
        }

        // POST api/order/{orderId}/decline
        [HttpPost("{orderId}/decline")]
        public async Task<IActionResult> DeclineOrder(int orderId)
        {
            try
            {
                var result = await _orderService.DeclineOrderAsync(orderId);
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    if (result.Message.Contains("Không tìm thấy đơn hàng"))
                        return NotFound(result);
                    if (result.Message.Contains("Không thể từ chối/hủy đơn hàng ở trạng thái"))
                        return BadRequest(result); // Wrong state

                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API DeclineOrder EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi từ chối đơn hàng." });
            }
        }

        //// GET api/order/my-orders
        ///// <summary>
        ///// Lấy danh sách các đơn hàng của khách hàng đang đăng nhập.
        ///// </summary>
        //[HttpGet("my-orders")]
        //[Authorize(Roles = "Customer")]
        //public async Task<IActionResult> GetMyOrders()
        //{
        //    try
        //    {
        //        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (!int.TryParse(userIdString, out var userId))
        //        {
        //            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ." });
        //        }

        //        // *** LƯU Ý: Interface IOrderService cần có hàm GetAllOrderAsync(int userId) ***
        //        var result = await _orderService.GetAllOrderAsync(userId); // Gọi hàm lấy theo UserId

        //        if (result.Success)
        //        {
        //            return Ok(result);
        //        }
        //        else
        //        {
        //            // Trường hợp này ít xảy ra nếu user hợp lệ, trừ khi có lỗi DB
        //            return BadRequest(result);
        //        }
        //    }
        //    catch (NotImplementedException) // Bắt lỗi nếu hàm chưa được implement
        //    {
        //        return StatusCode(501, new ApiResponse<object> { Success = false, Message = "Chức năng lấy danh sách đơn hàng chưa được hoàn thiện." });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[API GetMyOrders EXCEPTION]: {ex.ToString()}");
        //        return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách đơn hàng." });
        //    }
        //}

        // GET api/order/{orderId}
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ." });
                }

                // Gọi service, service sẽ tự kiểm tra quyền dựa trên userId và role (cần implement logic này trong service)
                var result = await _orderService.GetOrderByIdAsync(orderId, userId);

                if (result.Success)
                {
                    // Kiểm tra xem có dữ liệu không (phòng trường hợp service trả về Success=true nhưng Data=null)
                    if (result.Data == null)
                    {
                        return NotFound(new ApiResponse<object> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId} hoặc bạn không có quyền xem." });
                    }
                    return Ok(result);
                }
                else
                {
                    // Service có thể trả về Success=false kèm message không có quyền
                    if (result.Message.Contains("không có quyền xem"))
                    {
                        return Forbid(); // Hoặc NotFound tùy cách bạn muốn ẩn thông tin
                    }
                    // Mặc định là NotFound nếu không tìm thấy
                    return NotFound(result);
                }
            }
            catch (NotImplementedException) // Bắt lỗi nếu hàm chưa được implement
            {
                return StatusCode(501, new ApiResponse<object> { Success = false, Message = "Chức năng lấy chi tiết đơn hàng chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrderById EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi hệ thống khi lấy chi tiết đơn hàng." });
            }
        }

        // --- CÁC ENDPOINT KHÁC CÓ THỂ CẦN CHO ADMIN/SELLER ---
        // Ví dụ:
        // [HttpGet]
        // [Authorize(Roles = "Admin,Seller")]
        // public async Task<IActionResult> GetAllOrdersForAdmin([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        // {
        //     // Gọi một hàm khác trong IOrderService, ví dụ: GetAllOrdersFilteredAsync(status, page, pageSize)
        //     // Service này sẽ không cần userId mà lấy tất cả hoặc lọc theo SellerId (nếu role là Seller)
        // }
    }
}