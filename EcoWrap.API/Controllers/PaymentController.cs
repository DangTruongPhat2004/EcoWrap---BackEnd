using EcoWrap.BLL.IServices;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderRepository _orderRepo;
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICustomerRepository _customerRepo;

        public PaymentController(
            IPaymentService paymentService,
            IOrderRepository orderRepo,
            ICartItemRepository cartItemRepo,
            IProductRepository productRepo,
            ICustomerRepository customerRepo) 
        {
            _paymentService = paymentService;
            _orderRepo = orderRepo;
            _cartItemRepo = cartItemRepo;
            _productRepo = productRepo;
            _customerRepo = customerRepo;
        }

        // Define a simple request model
        public class CreatePaymentLinkRequest
        {
            public int OrderId { get; set; }
            // You might want to add ReturnUrl and CancelUrl here if they vary
            // public string ReturnUrl { get; set; } = "YOUR_DEFAULT_RETURN_URL";
            // public string CancelUrl { get; set; } = "YOUR_DEFAULT_CANCEL_URL";
        }

        [HttpPost("create-link")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                // Validate OrderId
                if (request.OrderId <= 0)
                {
                    return BadRequest(new { Success = false, Message = "OrderId không hợp lệ." });
                }

                // Get userId from token to verify ownership (optional but recommended)
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("User ID không hợp lệ.");
                }

                // Fetch Order details
                var order = await _orderRepo.GetByIdAsync(request.OrderId);
                if (order == null)
                {
                    return NotFound(new { Success = false, Message = $"Không tìm thấy đơn hàng ID: {request.OrderId}" });
                }

                // You might need to adjust your repository/service to include Customer/User info when getting an order.
                var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
                if (customer == null || customer.UserId != userId)
                {
                    // Ghi log bảo mật ở đây nếu cần
                    Console.WriteLine($"[Security Warning] User {userId} attempted to create payment link for Order {request.OrderId} owned by Customer {order.CustomerId} (User {customer?.UserId}).");
                    return Forbid(); // Trả về 403 Forbidden - Không có quyền
                }

                // Ensure order is still pending
                if (order.OrderStatus != "Pending")
                {
                    return BadRequest(new { Success = false, Message = $"Đơn hàng {request.OrderId} không ở trạng thái chờ thanh toán ({order.OrderStatus})." });
                }

                if (order.TotalPrice == null || order.TotalPrice <= 0)
                {
                    return BadRequest(new { Success = false, Message = "Tổng giá trị đơn hàng không hợp lệ." });
                }

                // Fetch CartItems for the order to create PayOS ItemData
                var cartItems = await _cartItemRepo.GetByOrderIdAsync(request.OrderId);
                if (!cartItems.Any())
                {
                    return BadRequest(new { Success = false, Message = "Đơn hàng không có sản phẩm." });
                }

                // Map CartItems to PayOS ItemData
                var itemsForPayOS = new List<ItemData>();
                foreach (var item in cartItems)
                {
                    // Fetch product name (optional but good for PayOS description)
                    var product = await _productRepo.GetByIdAsync(item.ProductId); // Assuming you have IProductRepository
                    string productName = product?.ProductName ?? $"Sản phẩm #{item.ProductId}";

                    itemsForPayOS.Add(new ItemData(
                        name: productName,
                        quantity: item.Quantity,
                        price: (int)item.CartItemPrice // PayOS expects int price per item
                    ));
                }

                // Define Return and Cancel URLs (can be from config or request)
                string returnUrl = "https://yourdomain.com/payment/success"; // Replace with your actual URL
                string cancelUrl = "https://yourdomain.com/payment/cancel";   // Replace with your actual URL

                // Call Payment Service
                var paymentLinkResponse = await _paymentService.CreatePaymentLinkAsync(
                    order.OrderId,
                    (long)order.TotalPrice.Value, // Convert decimal to long for PayOS amount
                    $"Thanh toán đơn hàng #{order.OrderId}",
                    itemsForPayOS,
                    returnUrl,
                    cancelUrl
                );

                // Return result
                if (paymentLinkResponse.Success)
                {
                    return Ok(new { Success = true, paymentLinkResponse.CheckoutUrl });
                }
                else
                {
                    // Log the error from paymentLinkResponse.ErrorMessage
                    Console.WriteLine($"[PayOS Link Error] OrderId {request.OrderId}: {paymentLinkResponse.ErrorMessage}");
                    return StatusCode(500, new { Success = false, Message = $"Lỗi khi tạo link thanh toán: {paymentLinkResponse.ErrorMessage}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API CreatePaymentLink EXCEPTION] OrderId {request?.OrderId}: {ex.Message}");
                return StatusCode(500, new { Success = false, Message = "Đã xảy ra lỗi hệ thống khi tạo link thanh toán." });
            }
        }
    }
}