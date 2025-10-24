using EcoWrap.BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        // Bỏ IOrderService vì không cần dùng ở đây nữa
        // private readonly IOrderService _orderService; 

        // Sửa Constructor: Chỉ cần IPaymentService
        public WebhookController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
            // _orderService = orderService; // Bỏ dòng này
        }

        [HttpPost("payos")]
        public async Task<IActionResult> HandlePayosWebhook()
        {
            string jsonBody = "";
            try
            {
                // 1. Đọc body
                using (var reader = new StreamReader(Request.Body))
                {
                    jsonBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"[PayOS Webhook] Received Body: {jsonBody}"); 
                }

                // 2. Đọc signature (Giữ nguyên)
                if (!Request.Headers.TryGetValue("x-payos-signature", out StringValues signatureValues))
                {
                    Console.WriteLine("[PayOS Webhook] Error: Missing 'x-payos-signature' header."); // Cập nhật log
                    return Ok("Missing signature header.");
                }
                string signature = signatureValues.FirstOrDefault() ?? "";
                Console.WriteLine($"[PayOS Webhook] Received Signature: {signature}");

                if (string.IsNullOrEmpty(signature))
                {
                    Console.WriteLine("[PayOS Webhook] Error: Empty 'x-payos-signature' header."); // Cập nhật log
                    return Ok("Empty signature header.");
                }

                // 3. Gọi Payment Service (Hàm này giờ đã bao gồm cả xử lý Order)
                var processingResult = await _paymentService.ProcessWebhookAsync(jsonBody, signature);

                // 4. Chỉ cần Log kết quả từ Payment Service (Không cần gọi OrderService nữa)
                if (processingResult.IsValidSignature)
                {
                    if (processingResult.IsSuccess)
                    {
                        // PayosService đã xử lý Order rồi, chỉ cần log ở đây
                        Console.WriteLine($"[PayOS Webhook] Signature VALID. Payment SUCCESS for OrderId: {processingResult.OrderId}. Order processing handled by PaymentService.");
                    }
                    else
                    {
                        Console.WriteLine($"[PayOS Webhook] Signature VALID but payment UNSUCCESSFUL/other status for OrderId: {processingResult.OrderId}. Message: {processingResult.ErrorMessage}");
                    }
                }
                else
                {
                    Console.WriteLine($"[PayOS Webhook ERROR] Invalid Signature. Message: {processingResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS Webhook EXCEPTION] General error processing webhook. Body received: {jsonBody}. Error: {ex.Message}");
            }

            // 5. Luôn trả về OK (Giữ nguyên)
            return Ok("Webhook received.");
        }
    }
}