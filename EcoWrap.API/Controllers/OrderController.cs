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

        // --- GET ALL ORDERS (ADMIN) ---
        // GET api/order/admin/all
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrdersAdmin()
        {
            Console.WriteLine("[API] Request received for GetAllOrdersAdmin."); // Add log
            try
            {
                // Use GetAllOrdersAdminAsync as implemented in the service
                var result = await _orderService.GetAllOrdersAdminAsync();
                if (result.Success)
                {
                    Console.WriteLine($"[API] GetAllOrdersAdmin succeeded, returning {result.Data?.Count ?? 0} orders."); // Add log
                    return Ok(result);
                }
                // Log service error before returning 500
                Console.WriteLine($"[API] GetAllOrdersAdmin failed in service: {result.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (NotImplementedException) // Catch if service method is not implemented
            {
                Console.WriteLine("[API] GetAllOrdersAdmin failed: Service method not implemented."); // Add log
                return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<object> { Success = false, Message = "Chức năng lấy tất cả đơn hàng chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetAllOrdersAdmin EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy tất cả đơn hàng." });
            }
        }

        // --- GET ORDERS BY USER ---
        // GET api/order/my-orders
        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            var unauthorizedResult = GetCurrentUserId(out int userId);
            if (unauthorizedResult != null) return unauthorizedResult;

            Console.WriteLine($"[API] Request received for GetMyOrders for User {userId}."); // Add log
            try
            {
                var result = await _orderService.GetAllOrdersByUserAsync(userId);
                if (result.Success)
                {
                    Console.WriteLine($"[API] GetMyOrders succeeded for User {userId}, returning {result.Data?.Count ?? 0} orders."); // Add log
                    return Ok(result);
                }
                Console.WriteLine($"[API] GetMyOrders failed in service for User {userId}: {result.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"[API] GetMyOrders failed for User {userId}: Service method not implemented."); // Add log
                return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<object> { Success = false, Message = "Chức năng lấy danh sách đơn hàng chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetMyOrders EXCEPTION] UserId {userId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy danh sách đơn hàng." });
            }
        }

        // --- GET ORDER BY ID (CUSTOMER / ADMIN / SELLER VIEW) ---
        // GET api/order/{orderId}
        [HttpGet("{orderId}")]
        [Authorize(Roles = "Customer,Admin,Seller")] // Authorization handled by service logic
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var unauthorizedResult = GetCurrentUserId(out int userId);
            if (unauthorizedResult != null) return unauthorizedResult;

            Console.WriteLine($"[API] Request received for GetOrderById {orderId} by User {userId}."); // Add log
            try
            {
                var result = await _orderService.GetOrderByIdAsync(orderId, userId);
                if (result.Success)
                {
                    if (result.Data == null) // Service might return success false + msg for not found/forbidden
                    {
                        Console.WriteLine($"[API] GetOrderById {orderId} by User {userId}: Not Found or Forbidden by service."); // Add log
                        return NotFound(new ApiResponse<object> { Success = false, Message = result.Message ?? $"Không tìm thấy đơn hàng ID: {orderId} hoặc không có quyền xem." });
                    }
                    Console.WriteLine($"[API] GetOrderById {orderId} by User {userId} succeeded."); // Add log
                    return Ok(result);
                }
                else
                {
                    // Handle specific errors returned by service
                    if (result.Message.Contains("không có quyền"))
                    {
                        Console.WriteLine($"[API] GetOrderById {orderId} by User {userId}: Forbidden by service."); // Add log
                        return Forbid(); // 403
                    }
                    Console.WriteLine($"[API] GetOrderById {orderId} by User {userId}: Not Found by service."); // Add log
                    return NotFound(result); // Default to NotFound if service fails
                }
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"[API] GetOrderById {orderId} by User {userId}: Service method not implemented."); // Add log
                return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<object> { Success = false, Message = "Chức năng lấy chi tiết đơn hàng chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrderById EXCEPTION] OrderId {orderId}, UserId {userId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy chi tiết đơn hàng." });
            }
        }

        // --- GET ORDER BY ID (ADMIN DETAIL VIEW W/ SELLER INFO) ---
        // GET api/order/admin/{orderId}
        [HttpGet("detail/{orderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrderByIdAdmin(int orderId)
        {
            Console.WriteLine($"[API] Request received for GetOrderByIdAdmin {orderId}."); // Add log
            try
            {
                var result = await _orderService.GetOrderByIdForAdminAsync(orderId);
                if (result.Success)
                {
                    if (result.Data == null) // Should not happen if order exists, but good check
                    {
                        Console.WriteLine($"[API] GetOrderByIdAdmin {orderId}: Not Found by service."); // Add log
                        return NotFound(new ApiResponse<object> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" });
                    }
                    Console.WriteLine($"[API] GetOrderByIdAdmin {orderId} succeeded."); // Add log
                    return Ok(result);
                }
                Console.WriteLine($"[API] GetOrderByIdAdmin {orderId} failed in service: {result.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, result); // Treat service failure as 500
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"[API] GetOrderByIdAdmin {orderId}: Service method not implemented."); // Add log
                return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<object> { Success = false, Message = "Chức năng lấy chi tiết đơn hàng Admin chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrderByIdAdmin EXCEPTION] OrderId {orderId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy chi tiết đơn hàng cho Admin." });
            }
        }

        // --- GET ORDERS BY STATUS (ADMIN) ---
        // GET api/order/admin/status/{status}
        /// <summary>
        /// (Admin) Lấy danh sách đơn hàng theo trạng thái.
        /// </summary>
        [HttpGet("detail/status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersByStatusAdmin(string status)
        {
            Console.WriteLine($"[API] Request received for GetOrdersByStatusAdmin with status '{status}'."); // Add log
            try
            {
                var result = await _orderService.GetOrdersByStatusAdminAsync(status);
                if (result.Success)
                {
                    Console.WriteLine($"[API] GetOrdersByStatusAdmin '{status}' succeeded, returning {result.Data?.Count ?? 0} orders."); // Add log
                    return Ok(result);
                }
                // Handle invalid status error specifically
                if (result.Message.Contains("không hợp lệ"))
                {
                    Console.WriteLine($"[API] GetOrdersByStatusAdmin failed: Invalid status '{status}'."); // Add log
                    return BadRequest(result);
                }
                // Other service errors
                Console.WriteLine($"[API] GetOrdersByStatusAdmin '{status}' failed in service: {result.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"[API] GetOrdersByStatusAdmin '{status}': Service method not implemented."); // Add log
                return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<object> { Success = false, Message = "Chức năng lấy đơn hàng theo trạng thái chưa được hoàn thiện." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrdersByStatusAdmin EXCEPTION] Status {status}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy đơn hàng theo trạng thái." });
            }
        }

        // --- HELPER: Get User ID from Token ---
        private IActionResult GetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out userId))
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ trong token." });
            }
            return null; // Success
        }
    }
}