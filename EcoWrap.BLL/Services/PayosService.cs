// File: BLL/Services/PayosService.cs
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Payment; // Needed for CreatePaymentResponse, WebhookProcessingResult
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Net.payOS;
using Net.payOS.Types;
using EcoWrap.DAL.IRepositories;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
// Thêm using cho ICartRepository nếu bạn muốn khóa Cart ở đây
// using EcoWrap.DAL.IRepositories;

namespace EcoWrap.BLL.Services
{
    public class PayosService : IPaymentService
    {
        private readonly EcoWrapDbContext _context;
        private readonly PayOS _payOS;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;

        public PayosService(
            EcoWrapDbContext context,
            IConfiguration configuration,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            ICartItemRepository cartItemRepository,
            IInventoryRepository inventoryRepository,
            IEmailService emailService,
            IBackgroundJobService backgroundJobService,
            ICustomerRepository customerRepository,
            IUserRepository userRepository
            )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            string clientId = configuration["PayOS:ClientId"] ?? throw new InvalidOperationException("Missing PayOS:ClientId configuration.");
            string apiKey = configuration["PayOS:ApiKey"] ?? throw new InvalidOperationException("Missing PayOS:ApiKey configuration.");
            string checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new InvalidOperationException("Missing PayOS:ChecksumKey configuration.");

            _payOS = new PayOS(clientId, apiKey, checksumKey);
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _cartItemRepository = cartItemRepository;
            _inventoryRepository = inventoryRepository;
            _emailService = emailService;
            _backgroundJobService = backgroundJobService;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
        }

        // --- Tạo Link Thanh toán ---
        public async Task<CreatePaymentResponse> CreatePaymentLinkAsync(
            int orderId, long amount, string description, List<ItemData> items,
            string returnUrl, string cancelUrl)
        {
            Console.WriteLine($"[PayOS SDK] Creating payment link for OrderId: {orderId}, Amount: {amount}");
            try
            {
                // --- Kiểm tra Payment cũ ---
                var oldPayment = await _paymentRepository.GetPaymentByOrderIdAsync(orderId);
                if (oldPayment != null)
                {
                    if (oldPayment.PaymentStatus == "Pending" || oldPayment.PaymentStatus == "Failed")
                    {
                        Console.WriteLine($"[PayOS SDK] Deleting old payment (Status={oldPayment.PaymentStatus}) for Order {orderId}");
                        _context.Payments.Remove(oldPayment);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return new CreatePaymentResponse
                        {
                            Success = false,
                            ErrorMessage = "Đơn hàng đã được thanh toán thành công, không thể tạo lại link."
                        };
                    }
                }

                // Mặc định items nếu null hoặc rỗng
                if (items == null || !items.Any())
                {
                    items = new List<ItemData> { new ItemData($"Đơn hàng EcoWrap #{orderId}", 1, (int)amount) };
                }

                // --- Sinh orderCodeLong tự động ---
                if (amount > int.MaxValue)
                    throw new InvalidOperationException("Amount vượt quá giới hạn PayOS int max.");

                long orderCodeLong = long.Parse($"{orderId}{DateTime.UtcNow:MMddHHmmss}");

                var paymentData = new PaymentData(
                    orderCode: orderCodeLong,
                    amount: (int)amount, // PayOS yêu cầu amount là int
                    description: description,
                    items: items,
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl
                // buyerName, buyerEmail, buyerPhone, buyerAddress có thể thêm nếu cần
                );

                Console.WriteLine($"[PayOS SDK] Calling createPaymentLink API with OrderCode: {paymentData.orderCode}, Amount: {paymentData.amount}");
                CreatePaymentResult sdkResult = await _payOS.createPaymentLink(paymentData);

                if (sdkResult != null && !string.IsNullOrEmpty(sdkResult.checkoutUrl))
                {
                    // *** LƯU THÔNG TIN PAYMENT BAN ĐẦU VÀO DB ***
                    var paymentRecord = new Payment
                    {
                        OrderId = orderId,
                        Amount = (decimal)amount, // Lưu decimal trong DB
                        PaymentStatus = "Pending", // Trạng thái ban đầu
                        PaymentLinkId = sdkResult.paymentLinkId, // Lưu Link ID
                        QrCode = sdkResult.qrCode, // Lưu QR Code
                        TransactionCode = null // Sẽ cập nhật từ webhook
                    };
                    await _paymentRepository.CreatePaymentAsync(paymentRecord); // Giả sử repo có hàm này
                    await _paymentRepository.SaveChangesAsync(); // Lưu vào DB
                    Console.WriteLine($"[PayOS SDK] Saved initial Payment record ID: {paymentRecord.PaymentId} for Order {orderId}.");

                    // Trả về kết quả cho Controller
                    return new CreatePaymentResponse
                    {
                        Success = true,
                        CheckoutUrl = sdkResult.checkoutUrl,
                        PaymentLinkId = sdkResult.paymentLinkId,
                        QrCode = sdkResult.qrCode
                    };
                }
                else
                {
                    Console.WriteLine($"[PayOS SDK ERROR] createPaymentLink returned null or empty checkoutUrl for Order {orderId}.");
                    return new CreatePaymentResponse { Success = false, ErrorMessage = "Không thể tạo link thanh toán từ PayOS." };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS SDK EXCEPTION] CreatePaymentLinkAsync for Order {orderId}: {ex.ToString()}");
                return new CreatePaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi hệ thống khi tạo link thanh toán: {ex.Message}"
                };
            }
        }

        // --- Xử lý Webhook Thanh toán ---
        public async Task<WebhookProcessingResult> ProcessWebhookAsync(string webhookJsonData, string signatureFromHeader)
        {
            Console.WriteLine("[PayOS SDK] Processing Webhook...");
            WebhookData verifiedWebhookBody;
            int orderId = 0; // Khởi tạo orderId
            bool isSuccess = false;
            string? transactionCode = null; // Mã giao dịch thực tế
            string? paymentLinkId = null;

            try
            {
                // --- BƯỚC 1 & 2: PARSE VÀ XÁC THỰC SIGNATURE ---
                using JsonDocument doc = JsonDocument.Parse(webhookJsonData);
                JsonElement root = doc.RootElement;
                string code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() ?? "" : "";
                string desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() ?? "" : "";
                JsonElement dataElement; // Không GetProperty trực tiếp nếu có thể null
                root.TryGetProperty("data", out dataElement); // Dùng TryGetProperty

                WebhookData? webhookDataObj = null;
                if (dataElement.ValueKind == JsonValueKind.Object) // Chỉ Deserialize nếu là object
                {
                    // Lấy các giá trị cần thiết trước khi Deserialize hoàn chỉnh
                    orderId = dataElement.TryGetProperty("orderCode", out var ocEl) && ocEl.ValueKind == JsonValueKind.Number ? ocEl.GetInt32() : 0;
                    transactionCode = dataElement.TryGetProperty("reference", out var refEl) ? refEl.GetString() : null; // *** LẤY MÃ GD THỰC TẾ TỪ 'reference' ***
                    paymentLinkId = dataElement.TryGetProperty("paymentLinkId", out var plidEl) ? plidEl.GetString() : null;

                    // Deserialize đầy đủ nếu cần các trường khác
                    webhookDataObj = JsonSerializer.Deserialize<WebhookData>(dataElement.GetRawText());
                    // Cập nhật lại các giá trị từ object đã deserialize để đảm bảo khớp
                    if (webhookDataObj != null)
                    {
                        orderId = (int)webhookDataObj.orderCode; // ép kiểu long về int
                        transactionCode = webhookDataObj.reference;
                        paymentLinkId = webhookDataObj.paymentLinkId;
                    }
                }

                isSuccess = (code == "00"); // Xác định trạng thái thành công dựa trên 'code' ngoài 'data'

                // Kiểm tra orderId hợp lệ trước khi xác thực signature
                if (orderId <= 0)
                {
                    throw new InvalidOperationException("Webhook data is missing or has invalid 'orderCode'.");
                }

                WebhookType webhookType = new WebhookType(code, desc, isSuccess, webhookDataObj, signatureFromHeader);

                // Nếu signature không hợp lệ, hàm verifyPaymentWebhookData sẽ ném ra Exception
                verifiedWebhookBody = _payOS.verifyPaymentWebhookData(webhookType);
                Console.WriteLine($"[PayOS SDK] Webhook Signature VERIFIED for OrderId: {orderId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS SDK EXCEPTION] ProcessWebhook Error (OrderId: {orderId}): {ex.GetType().Name} - {ex.Message}");
                // Ghi log chi tiết hơn nếu cần: ex.ToString()
                bool isSignatureError = ex.Message.ToLowerInvariant().Contains("signature") || ex.Message.ToLowerInvariant().Contains("unreliable");

                return new WebhookProcessingResult
                {
                    OrderId = orderId, // Trả về orderId nếu có thể lấy được
                    IsValidSignature = !isSignatureError,
                    IsSuccess = false, // Luôn là false nếu có lỗi ở bước này
                    ErrorMessage = $"Lỗi xử lý webhook PayOS: {ex.Message}"
                };
            }

            // --- BƯỚC 3: CẬP NHẬT DỮ LIỆU ---
            // Đã qua xác thực signature, verifiedWebhookBody không null (nếu null đã throw exception)

            // Lấy lại các giá trị đã parse ở trên hoặc từ verifiedWebhookBody nếu cần
            // orderId = (int)verifiedWebhookBody.orderCode;
            // paymentLinkId = verifiedWebhookBody.paymentLinkId;
            // transactionCode = verifiedWebhookBody.reference; // Lấy mã GD thực tế
            isSuccess = (verifiedWebhookBody.code == "00"); // Trạng thái thành công cuối cùng từ data
            string paymentStatusInDb = isSuccess ? "Completed" : "Failed";

            // *** 3.1. Cập nhật trạng thái Payment trong DB ***
            try
            {
                // Giả sử UpdatePaymentStatusAsync tìm theo PaymentLinkId
                if (!string.IsNullOrEmpty(paymentLinkId))
                {
                    bool paymentUpdated = await _paymentRepository.UpdatePaymentStatusAsync(
                        paymentLinkId,
                        paymentStatusInDb,
                        transactionCode // Lưu mã GD thực tế
                    );
                    await _paymentRepository.SaveChangesAsync(); // Lưu thay đổi Payment

                    if (!paymentUpdated)
                    {
                        // Lỗi này khá nghiêm trọng, vì link thanh toán đã tồn tại trước đó
                        Console.WriteLine($"[PayOS SDK ERROR - Action Required] Order {orderId}: Could not find Payment Record with Link ID: {paymentLinkId} to update status.");
                        // Có thể throw exception ở đây để báo lỗi nghiêm trọng? Hoặc chỉ log và tiếp tục? -> Chỉ log
                    }
                    else
                    {
                        Console.WriteLine($"[PayOS SDK] Updated Payment Record for Link ID {paymentLinkId} to status: {paymentStatusInDb} with TransactionCode: {transactionCode}");
                    }
                }
                else
                {
                    Console.WriteLine($"[PayOS SDK WARNING] Order {orderId}: PaymentLinkId is missing in webhook data. Cannot update Payment status by LinkId.");
                    // Có thể thử tìm Payment record theo OrderId nếu cần, nhưng kém chính xác hơn
                }
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"[PayOS SDK DB EXCEPTION] Order {orderId}: Error updating Payment status. Error: {dbEx.Message}");
                // Không ném lỗi ra ngoài để transaction xử lý Order vẫn có thể chạy (nếu isSuccess = true)
            }


            // *** 3.2. Cập nhật Order Status và Inventory (Nếu thanh toán thành công) ***
            if (isSuccess)
            {
                Console.WriteLine($"[PayOS SDK] Payment SUCCESS for Order #{orderId}. Processing order (Status -> Packaging, Deduct Inventory)...");
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null)
                            throw new InvalidOperationException($"Order ID {orderId} not found after successful payment.");

                        // Idempotency Check
                        if (order.OrderStatus != "Pending")
                        {
                            Console.WriteLine($"[Webhook Idempotency] Order {orderId} already processed (Status: {order.OrderStatus}). Skipping DB updates.");
                            scope.Complete(); // Hoàn tất transaction trống
                        }
                        else
                        {
                            var orderItems = await _cartItemRepository.GetByOrderIdAsync(orderId);
                            if (!orderItems.Any())
                                throw new InvalidOperationException($"Order {orderId} has no items after successful payment.");

                            // Trừ tồn kho
                            Console.WriteLine($"[PayOS SDK] Deducting inventory for Order {orderId}...");
                            foreach (var item in orderItems)
                            {
                                var deductSuccess = await _inventoryRepository.DecreaseStockAcrossWarehousesAsync(item.ProductId, item.Quantity);
                                if (!deductSuccess)
                                    throw new InvalidOperationException($"Insufficient stock for Product ID {item.ProductId} in Order {orderId} after payment.");
                            }
                            await _inventoryRepository.SaveChangesAsync(); // Lưu thay đổi tồn kho
                            Console.WriteLine($"[PayOS SDK] Inventory deducted successfully for Order {orderId}.");

                            // Cập nhật trạng thái Order -> Packaging
                            order.OrderStatus = "Packaging"; // *** ĐỔI THÀNH PACKAGING ***
                            order.UpdatedDate = DateTime.UtcNow;
                            // Không cần UpdateAsync nếu repo dùng DbContext tracking
                            await _orderRepository.SaveChangesAsync(); // Lưu thay đổi Order
                            Console.WriteLine($"[PayOS SDK] Order {orderId} status updated to Packaging.");

                            // (Tùy chọn) Khóa Cart nếu chưa làm ở CreateOrderAsync
                            // var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                            // var cart = await _cartRepository.GetActiveCartByCustomerIdAsync(customer.CustomerId);
                            // if (cart != null && cart.IsActive) {
                            //    cart.IsActive = false;
                            //    await _cartRepository.SaveChangesAsync();
                            //    Console.WriteLine($"[PayOS SDK] Cart {cart.CartId} deactivated for Customer {customer.CustomerId}.");
                            // }

                            scope.Complete(); // Commit transaction
                            Console.WriteLine($"[PayOS SDK] Order #{orderId} processing committed (Packaging, Inventory deducted).");

                            // *** GỬI EMAIL XÁC NHẬN THANH TOÁN (Sau Transaction) ***
                            try
                            {
                                var customer = await _customerRepository.GetByIdAsync(order.CustomerId); // Lấy lại customer
                                if (customer != null)
                                {
                                    var userForEmail = await _userRepository.GetByIdAsync(customer.UserId);
                                    if (userForEmail != null)
                                    {
                                        string subject = $"EcoWrap: Xác nhận thanh toán đơn hàng #{orderId}";
                                        string body = $"Chào {userForEmail.FullName}, Thanh toán cho đơn hàng #{orderId} của bạn đã thành công. Đơn hàng đang được chúng tôi chuẩn bị.";
                                        _backgroundJobService.Enqueue<IEmailService>(
                                            email => email.SendEmailAsync(userForEmail.Email, subject, body)
                                        );
                                        Console.WriteLine($"[PayOS SDK] Enqueued payment confirmation email for Order {orderId}.");
                                    }
                                }
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"[PayOS SDK EMAIL Error] Order {orderId}: Failed to enqueue confirmation email: {emailEx.Message}");
                            }
                        } // End else (order was Pending)
                    } // End try
                    catch (Exception ex)
                    {
                        // Lỗi nghiêm trọng trong transaction (không tìm thấy order, hết hàng, lỗi DB...)
                        Console.WriteLine($"[PAYOS WEBHOOK TRANSACTION EXCEPTION - ACTION REQUIRED] Order #{orderId}: Critical error processing order after payment: {ex.Message}");
                        // KHÔNG scope.Complete(); Transaction sẽ tự rollback
                        // Cần admin kiểm tra và có thể cần hoàn tiền thủ công
                        isSuccess = false; // Đánh dấu lại là xử lý cuối cùng không thành công hoàn toàn
                        // Không ném lỗi ra ngoài để hàm vẫn trả về kết quả cho Controller
                    }
                } // End using TransactionScope
            } // End if(isSuccess)
            else // Trường hợp webhook báo thanh toán thất bại (code != "00")
            {
                Console.WriteLine($"[PayOS SDK] Payment FAILED for Order #{orderId}. Status: {verifiedWebhookBody.code} - {verifiedWebhookBody.desc}. No order processing needed.");
                // Có thể lên lịch hủy đơn hàng ngay lập tức hoặc sau một thời gian ngắn nếu muốn
                // try {
                //     _backgroundJobService.Schedule<IOrderService>(s => s.AutoCancelOrderAsync(orderId), TimeSpan.FromMinutes(5));
                // } catch (Exception jobEx) { Console.WriteLine($"Failed to schedule cancel job for failed payment Order {orderId}: {jobEx.Message}"); }
            }

            // --- TRẢ KẾT QUẢ VỀ CONTROLLER ---
            Console.WriteLine($"[PayOS SDK] Finished processing webhook for OrderId: {orderId}. Final IsSuccess: {isSuccess}");
            return new WebhookProcessingResult
            {
                IsValidSignature = true, // Đã qua bước xác thực
                IsSuccess = isSuccess, // Trạng thái thanh toán cuối cùng (có thể false nếu rollback)
                OrderId = orderId,
                TransactionCode = transactionCode
                // ErrorMessage sẽ null nếu không có lỗi ở bước xác thực signature
            };
        }
    }
}