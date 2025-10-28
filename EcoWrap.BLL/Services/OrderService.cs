// File: BLL/Services/OrderService.cs
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request; // Giả sử bạn có CreateOrderRequest ở đây
using EcoWrap.Models.Response; // Giả sử bạn có OrderResponse ở đây
using EcoWrap.Models.Responses; // Giả sử bạn có ApiResponse ở đây
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;

namespace EcoWrap.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly EcoWrapDbContext _dbContext;
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IUserRepository _userRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IGhtkService _ghtkService;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            EcoWrapDbContext dbContext,
            IOrderRepository orderRepo,
            ICartRepository cartRepo,
            ICartItemRepository cartItemRepo,
            IUserRepository userRepo,
            ICustomerRepository customerRepo,
            IGhtkService ghtkService,
            IInventoryRepository inventoryRepo,
            IEmailService emailService,
            IBackgroundJobService backgroundJobService,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _dbContext = dbContext;
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _userRepo = userRepo;
            _customerRepo = customerRepo;
            _ghtkService = ghtkService;
            _inventoryRepo = inventoryRepo;
            _emailService = emailService;
            _backgroundJobService = backgroundJobService;
            _httpContextAccessor = httpContextAccessor;
        }

        // --- Bước 1: Tạo Order (Trạng thái Pending) ---
        public async Task<ApiResponse<OrderResponse>> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            // 1. Lấy thông tin User và Cart (Kiểm tra cart hợp lệ, có items)
            var customer = await _customerRepo.GetByUserIdAsync(userId);
            if (customer == null)
                return new ApiResponse<OrderResponse> { Success = false, Message = "Không tìm thấy thông tin khách hàng." };

            var cart = await _cartRepo.GetActiveCartByCustomerIdAsync(customer.CustomerId);
            if (cart == null || cart.IsActive != true)
                return new ApiResponse<OrderResponse> { Success = false, Message = "Không tìm thấy giỏ hàng hoạt động." };

            var cartItems = await _cartItemRepo.GetByCartIdAsync(cart.CartId);
            if (!cartItems.Any())
                return new ApiResponse<OrderResponse> { Success = false, Message = "Giỏ hàng trống." };

            // 2. Tính toán
            var itemCount = cartItems.Sum(item => item.Quantity); // Nên sum quantity thay vì count rows
            var totalPrice = cartItems.Sum(item => item.CartItemPrice);

            // 3. Tạo Order Entity
            var newOrder = new Order
            {
                CustomerId = customer.CustomerId,
                TotalAmount = itemCount, // Số lượng sản phẩm
                TotalPrice = totalPrice, // Tổng giá tiền
                Address = request.Address,
                Province = request.Province,
                District = request.District,
                Ward = request.Ward,
                Hamlet = request.Hamlet,
                OrderStatus = "Pending", // Trạng thái ban đầu
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
                // ShippingCode, TransportUnit sẽ null ban đầu
            };

            // 4. Lưu Order, Cập nhật CartItems, Khóa Cart trong Transaction
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // 4.1. Lưu Order để lấy OrderId
                await _orderRepo.AddAsync(newOrder);
                await _orderRepo.SaveChangesAsync(); // Cần Save để có newOrder.OrderId

                // 4.2. Gán OrderId cho CartItems
                foreach (var item in cartItems)
                {
                    item.OrderId = newOrder.OrderId;
                    // Không cần gọi UpdateAsync nếu _cartItemRepo dùng DbContext và tracking
                    // await _cartItemRepo.UpdateAsync(item);
                }
                await _cartItemRepo.SaveChangesAsync(); // Lưu thay đổi OrderId trên CartItems

                // 4.3. Khóa Cart cũ
                cart.IsActive = false;
                // Không cần gọi UpdateAsync nếu _cartRepo dùng DbContext và tracking
                // await _cartRepo.UpdateAsync(cart);
                await _cartRepo.SaveChangesAsync(); // Lưu trạng thái IsActive của Cart

                scope.Complete(); // Commit transaction
            }

            // 5. Lên lịch tự động hủy sau 30 phút
            try
            {
                _backgroundJobService.Schedule<IOrderService>(
                    service => service.AutoCancelOrderAsync(newOrder.OrderId),
                    TimeSpan.FromMinutes(30) // Hoặc lấy từ config
                );
                Console.WriteLine($"[OrderService] Scheduled auto-cancel job for Order {newOrder.OrderId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hangfire Error] Failed to schedule cancel job for Order {newOrder.OrderId}: {ex.Message}");
                // Log lỗi nhưng vẫn tiếp tục trả về thành công cho việc tạo Order
            }

            // 6. Trả về thông tin Order đã tạo
            var responseData = MapOrderToOrderResponse(newOrder);
            return new ApiResponse<OrderResponse>
            {
                Success = true,
                Message = "Tạo đơn hàng thành công. Vui lòng thanh toán trong vòng 30 phút.",
                Data = responseData
            };
        }

        // --- Bước 2: Xác nhận Thanh toán và Đóng gói Thủ công---
        public async Task<ApiResponse<bool>> ManuallyConfirmPaymentAndPackageAsync(int orderId)
        {
            Console.WriteLine($"[OrderService] Attempting to manually confirm payment and package OrderId: {orderId}");
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    // 1. Lấy Order
                    var order = await _orderRepo.GetByIdAsync(orderId);
                    if (order == null)
                    {
                        return new ApiResponse<bool> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" };
                    }

                    // 2. Chỉ xử lý đơn hàng đang ở trạng thái Pending
                    if (order.OrderStatus != "Pending")
                    {
                        return new ApiResponse<bool> { Success = false, Message = $"Đơn hàng không ở trạng thái 'Pending'. Trạng thái hiện tại: {order.OrderStatus}. Không thể xác nhận thủ công." };
                    }

                    // 3. Lấy Order Items (CartItems)
                    var orderItems = await _cartItemRepo.GetByOrderIdAsync(orderId);
                    if (!orderItems.Any())
                    {
                        // Trường hợp này không nên xảy ra nếu đơn hàng hợp lệ
                        throw new InvalidOperationException($"Đơn hàng {orderId} không có sản phẩm nào. Không thể xử lý.");
                    }

                    // 4. Trừ tồn kho
                    Console.WriteLine($"[OrderService] Deducting inventory for manual confirmation of Order {orderId}...");
                    foreach (var item in orderItems)
                    {
                        var deductSuccess = await _inventoryRepo.DecreaseStockAcrossWarehousesAsync(item.ProductId, item.Quantity);
                        if (!deductSuccess)
                        {
                            // Nếu không đủ kho -> Không cho phép xác nhận thủ công, transaction sẽ rollback
                            throw new InvalidOperationException($"Sản phẩm ID {item.ProductId} không đủ tồn kho. Không thể xác nhận đơn hàng.");
                        }
                    }
                    await _inventoryRepo.SaveChangesAsync(); // Lưu thay đổi tồn kho
                    Console.WriteLine($"[OrderService] Inventory deducted for Order {orderId}.");

                    // 5. Cập nhật trạng thái Order -> Packaging
                    order.OrderStatus = "Packaging";
                    order.UpdatedDate = DateTime.UtcNow;
                    // Không cần UpdateAsync nếu repo dùng DbContext tracking
                    await _orderRepo.SaveChangesAsync(); // Lưu thay đổi trạng thái Order
                    Console.WriteLine($"[OrderService] Order {orderId} status updated to Packaging.");

                    // 6. Hoàn tất Transaction
                    scope.Complete();
                    Console.WriteLine($"[OrderService] Manual confirmation for Order {orderId} committed.");

                } // Kết thúc TransactionScope

                // 7. (Tùy chọn) Gửi Email thông báo (có thể giống email webhook)
                try
                {
                    var orderForEmail = await _orderRepo.GetByIdAsync(orderId); // Lấy lại order sau khi commit
                    var customer = await _customerRepo.GetByIdAsync(orderForEmail.CustomerId);
                    if (customer != null)
                    {
                        var userForEmail = await _userRepo.GetByIdAsync(customer.UserId);
                        if (userForEmail != null)
                        {
                            string subject = $"EcoWrap: Xác nhận thanh toán đơn hàng #{orderId}";
                            string body = $"Chào {userForEmail.FullName}, Thanh toán cho đơn hàng #{orderId} của bạn đã được xác nhận thủ công. Đơn hàng đang được chúng tôi chuẩn bị.";
                            _backgroundJobService.Enqueue<IEmailService>(
                                email => email.SendEmailAsync(userForEmail.Email, subject, body)
                            );
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"[OrderService EMAIL Error] Order {orderId}: Failed to enqueue manual confirmation email: {emailEx.Message}");
                }

                return new ApiResponse<bool> { Success = true, Message = "Đã xác nhận thanh toán và chuyển đơn hàng sang trạng thái đóng gói thành công." };
            }
            catch (InvalidOperationException ex) // Bắt lỗi nghiệp vụ cụ thể (hết hàng, sai trạng thái...)
            {
                Console.WriteLine($"[OrderService EXCEPTION] ManualConfirmPayment Order {orderId}: {ex.Message}");
                return new ApiResponse<bool> { Success = false, Message = ex.Message }; // Trả về lỗi nghiệp vụ
            }
            catch (Exception ex) // Bắt lỗi hệ thống khác
            {
                Console.WriteLine($"[OrderService SYSTEM EXCEPTION] ManualConfirmPayment Order {orderId}: {ex.ToString()}");
                return new ApiResponse<bool> { Success = false, Message = "Lỗi hệ thống khi xác nhận đơn hàng thủ công." };
            }
        }

        // --- Bước 3: Trung gian Xác nhận Giao hàng (Trạng thái Shipping) ---
        public async Task<ApiResponse<GhtkResponse>> ConfirmShipmentByAdminAsync(int orderId) // <-- SỬA KIỂU TRẢ VỀ
        {
            Order order;
            User userForEmail;

            try
            {
                order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null)
                    return new ApiResponse<GhtkResponse> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" };

                // *** Chỉ cho phép chuyển từ Packaging ***
                if (order.OrderStatus != "Packaging")
                {
                    return new ApiResponse<GhtkResponse> { Success = false, Message = $"Đơn hàng không ở trạng thái 'Packaging'. Trạng thái hiện tại: {order.OrderStatus}" };
                }

                // Lấy thông tin khách hàng và user để gửi mail/GHTK
                var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
                if (customer == null) throw new InvalidOperationException($"Không tìm thấy khách hàng cho Order {orderId}.");
                userForEmail = await _userRepo.GetByIdAsync(customer.UserId);
                if (userForEmail == null) throw new InvalidOperationException($"Không tìm thấy người dùng cho Customer {customer.CustomerId}.");


                // *** GỌI GHTK ĐỂ TẠO VẬN ĐƠN (Trực tiếp) ***
                Console.WriteLine($"[OrderService] Calling GHTK CreateShipment for Order {orderId}");
                var ghtkResponse = await _ghtkService.CreateShipmentAsync(order, userForEmail);

                if (ghtkResponse.Success && !string.IsNullOrEmpty(ghtkResponse.ShippingCode))
                {
                    // *** Cập nhật Order với thông tin GHTK và đổi Status -> Shipping ***
                    order.TransportUnit = ghtkResponse.TransportUnit;
                    order.ShippingCode = ghtkResponse.ShippingCode;
                    order.OrderStatus = "Shipping"; // <-- Đổi Status
                    order.UpdatedDate = DateTime.UtcNow;

                    // Không cần gọi UpdateAsync nếu repo dùng DbContext tracking
                    await _orderRepo.SaveChangesAsync(); // Lưu thay đổi vào DB
                    Console.WriteLine($"[OrderService] Order {orderId} updated: Status -> Shipping, ShippingCode: {order.ShippingCode}");

                    // *** LÊN LỊCH GỬI EMAIL THÔNG BÁO GIAO HÀNG (Background) ***
                    try
                    {
                        string subject = $"EcoWrap: Đơn hàng #{orderId} đang được giao";
                        string body = $@"<html><body>
                                         <p>Chào {userForEmail.FullName},</p>
                                         <p>Tin vui! Đơn hàng <strong>#{orderId}</strong> của bạn đã được bàn giao cho {order.TransportUnit ?? "đơn vị vận chuyển"}.</p>
                                         <p>Mã vận đơn của bạn là: <strong>{order.ShippingCode}</strong></p>
                                         <p>Bạn có thể theo dõi đơn hàng tại [Link theo dõi GHTK nếu có].</p>
                                         <p>Cảm ơn,</p><p>Đội ngũ EcoWrap.</p>
                                         </body></html>";

                        _backgroundJobService.Enqueue<IEmailService>(
                            service => service.SendEmailAsync(userForEmail.Email, subject, body)
                        );
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[Hangfire Error] Failed to enqueue Shipping Email for Order {orderId}: {emailEx.Message}");
                    }

                    return new ApiResponse<GhtkResponse> { Success = true, Data = ghtkResponse, Message = "Đã xác nhận giao hàng và tạo vận đơn GHTK thành công." };
                }
                else
                {
                    // Lỗi khi gọi GHTK
                    Console.WriteLine($"[GHTK ERROR] Failed to create shipment for Order {orderId}: {ghtkResponse.Message}");
                    return new ApiResponse<GhtkResponse> { Success = false, Message = $"Lỗi khi tạo vận đơn GHTK: {ghtkResponse.Message}" };
                }
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác (VD: DB connection, null reference...)
                Console.WriteLine($"[OrderService EXCEPTION] ConfirmShipmentByAdminAsync for Order {orderId}: {ex.GetType().Name} - {ex.Message}");
                // Ghi log chi tiết hơn nếu cần: ex.ToString()
                return new ApiResponse<GhtkResponse> { Success = false, Message = "Lỗi hệ thống khi xác nhận giao hàng." };
            }
        }

        // --- Bước 4: Khách hàng Xác nhận (Trạng thái Completed) ---
        public async Task<ApiResponse<bool>> ConfirmDeliveryAsync(int orderId, int userId)
        {
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null)
                    return new ApiResponse<bool> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" };

                // Xác thực người dùng
                var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
                if (customer == null || customer.UserId != userId)
                    return new ApiResponse<bool> { Success = false, Message = "Bạn không có quyền xác nhận đơn hàng này." };

                // Kiểm tra trạng thái
                if (order.OrderStatus != "Shipping")
                    return new ApiResponse<bool> { Success = false, Message = $"Đơn hàng không ở trạng thái 'Shipping'. Trạng thái hiện tại: {order.OrderStatus}" };

                // Cập nhật trạng thái
                order.OrderStatus = "Completed";
                order.UpdatedDate = DateTime.UtcNow;

                // Không cần UpdateAsync nếu repo dùng DbContext tracking
                await _orderRepo.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Đã xác nhận nhận hàng thành công." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderService EXCEPTION] ConfirmDeliveryAsync for Order {orderId}, User {userId}: {ex.Message}");
                return new ApiResponse<bool> { Success = false, Message = "Lỗi hệ thống khi xác nhận nhận hàng." };
            }
        }

        // --- Ngoại lệ 1 & 2: Tự động Hủy (Trạng thái Canceled) ---
        // (Dùng cho cả chưa TT và có thể cả TT thất bại nếu PayOS không báo webhook fail)
        public async Task AutoCancelOrderAsync(int orderId)
        {
            Console.WriteLine($"[Background Job] Running AutoCancelOrderAsync for OrderId: {orderId}");
            User userForEmail = null; // Khai báo ngoài để dùng cho email
            bool inventoryRestored = false;

            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var order = await _orderRepo.GetByIdAsync(orderId);

                    // Chỉ hủy đơn đang ở trạng thái Pending
                    if (order == null || order.OrderStatus != "Pending")
                    {
                        Console.WriteLine($"[AUTO CANCEL IGNORED] Order {orderId} not found or status is '{order?.OrderStatus}'. No action taken.");
                        // Không cần Complete vì không làm gì
                        return; // Kết thúc job
                    }

                    // Lấy thông tin User để gửi email
                    var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
                    if (customer != null) // Nên kiểm tra null phòng trường hợp dữ liệu lỗi
                    {
                        userForEmail = await _userRepo.GetByIdAsync(customer.UserId);
                    }
                    // Nếu không tìm thấy user thì vẫn hủy đơn nhưng không gửi mail được

                    Console.WriteLine($"[AUTO CANCEL EXECUTING] Order {orderId} is Pending. Changing status to Canceled and restoring inventory...");

                    // *** KHÔI PHỤC TỒN KHO ***
                    var orderItems = await _cartItemRepo.GetByOrderIdAsync(orderId);
                    if (orderItems.Any())
                    {
                        Console.WriteLine($"[AUTO CANCEL] Restoring {orderItems.Sum(i => i.Quantity)} items for {orderItems.Count()} product lines in Order {orderId}...");
                        foreach (var item in orderItems)
                        {
                            try
                            {
                                await _inventoryRepo.IncreaseStockInFirstWarehouseAsync(item.ProductId, item.Quantity);
                            }
                            catch (Exception invEx)
                            {
                                // Ghi log lỗi nghiêm trọng khi cộng kho thất bại
                                Console.WriteLine($"[AUTO CANCEL INVENTORY ERROR - Action Required] Order {orderId}, Product {item.ProductId}: Failed to restore stock. Error: {invEx.Message}");
                                // Cân nhắc: Có nên dừng hủy đơn không? Hay vẫn hủy và báo lỗi? -> Vẫn hủy và báo lỗi
                            }
                        }
                        await _inventoryRepo.SaveChangesAsync(); // Lưu lại tồn kho (dù có lỗi ở item nào đó hay không)
                        inventoryRestored = true; // Ghi nhận đã cố gắng khôi phục
                        Console.WriteLine($"[AUTO CANCEL] Inventory restoration attempt finished for Order {orderId}.");
                    }
                    else
                    {
                        Console.WriteLine($"[AUTO CANCEL WARNING] Order {orderId} has no items. No inventory to restore.");
                        inventoryRestored = true; // Coi như không cần khôi phục -> thành công
                    }

                    // *** Cập nhật trạng thái Order -> Canceled ***
                    order.OrderStatus = "Canceled";
                    order.UpdatedDate = DateTime.UtcNow;
                    // Không cần UpdateAsync nếu repo dùng DbContext tracking
                    await _orderRepo.SaveChangesAsync(); // Lưu trạng thái Order

                    // Chỉ Complete transaction nếu việc khôi phục tồn kho không gặp lỗi nghiêm trọng (hoặc không cần khôi phục)
                    // (Hiện tại đang Complete bất kể lỗi cộng kho để đảm bảo đơn bị hủy)
                    scope.Complete(); // Commit transaction
                    Console.WriteLine($"[AUTO CANCEL COMPLETED] Order {orderId} status set to Canceled.");

                } // Kết thúc TransactionScope
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO CANCEL FAILED] Error during AutoCancelOrderAsync for Order {orderId}: {ex.Message}");
                // Transaction tự động rollback
                // Cần cơ chế retry hoặc thông báo lỗi nghiêm trọng
                // throw; // Ném lại lỗi để Hangfire retry nếu cấu hình
                return; // Hoặc chỉ return để job kết thúc
            }

            // *** GỬI EMAIL THÔNG BÁO HỦY TỰ ĐỘNG (Sau Transaction) ***
            if (userForEmail != null) // Chỉ gửi mail nếu tìm thấy user
            {
                try
                {
                    string subject = $"EcoWrap: Đơn hàng #{orderId} đã bị hủy";
                    string body = $@"<html><body>
                                    <p>Chào {userForEmail.FullName},</p>
                                    <p>Đơn hàng <strong>#{orderId}</strong> của bạn đã bị hủy tự động do quá hạn thanh toán.</p>
                                    {(inventoryRestored ? "<p>Số lượng sản phẩm đã được hoàn lại kho.</p>" : "<p>Lưu ý: Có thể đã xảy ra lỗi khi hoàn lại số lượng sản phẩm vào kho, vui lòng liên hệ hỗ trợ nếu cần.</p>")}
                                    <p>Nếu bạn cần hỗ trợ, vui lòng liên hệ chúng tôi.</p>
                                    <p>Cảm ơn,</p><p>Đội ngũ EcoWrap.</p>
                                    </body></html>";

                    _backgroundJobService.Enqueue<IEmailService>(
                        service => service.SendEmailAsync(userForEmail.Email, subject, body)
                    );
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"[Hangfire Error] Failed to enqueue Auto-Cancel Email for OrderId {orderId}: {emailEx.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[AUTO CANCEL WARNING] Could not find user for Order {orderId}. Cannot send cancellation email.");
            }
        }

        // --- Ngoại lệ 3: Hủy/Từ chối Thủ công (Trạng thái Declined) ---
        public async Task<ApiResponse<bool>> DeclineOrderAsync(int orderId /*, int adminUserId - thêm nếu cần check quyền*/)
        {
            User userForEmail;
            string? shippingCodeToCancel = null;
            bool inventoryRestored = false;

            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var order = await _orderRepo.GetByIdAsync(orderId);
                    if (order == null)
                        return new ApiResponse<bool> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" };

                    var originalStatus = order.OrderStatus;

                    // Kiểm tra trạng thái có thể hủy/từ chối
                    if (originalStatus == "Completed" || originalStatus == "Canceled" || originalStatus == "Declined")
                    {
                        return new ApiResponse<bool> { Success = false, Message = $"Không thể từ chối/hủy đơn hàng ở trạng thái '{originalStatus}'." };
                    }

                    // Lấy thông tin User để gửi email
                    var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
                    if (customer == null) throw new InvalidOperationException($"Không tìm thấy khách hàng cho Order {orderId} khi từ chối.");
                    userForEmail = await _userRepo.GetByIdAsync(customer.UserId);
                    if (userForEmail == null) throw new InvalidOperationException($"Không tìm thấy người dùng cho Customer {customer.CustomerId} khi từ chối.");


                    // *** KHÔI PHỤC TỒN KHO (NẾU CẦN) ***
                    if (originalStatus == "Packaging" || originalStatus == "Shipping")
                    {
                        var orderItems = await _cartItemRepo.GetByOrderIdAsync(orderId);
                        if (orderItems.Any())
                        {
                            Console.WriteLine($"[DECLINE ORDER] Restoring inventory for Order {orderId} (Original Status: {originalStatus})...");
                            foreach (var item in orderItems)
                            {
                                try
                                {
                                    await _inventoryRepo.IncreaseStockInFirstWarehouseAsync(item.ProductId, item.Quantity);
                                }
                                catch (Exception invEx)
                                {
                                    Console.WriteLine($"[DECLINE ORDER INVENTORY ERROR - Action Required] Order {orderId}, Product {item.ProductId}: Failed to restore stock. Error: {invEx.Message}");
                                    // Nên throw lỗi và rollback transaction ở đây không? Hay vẫn Decline? -> Ưu tiên Decline và báo lỗi
                                    // throw; // Nếu muốn rollback hoàn toàn
                                }
                            }
                            await _inventoryRepo.SaveChangesAsync(); // Lưu lại tồn kho
                            inventoryRestored = true;
                            Console.WriteLine($"[DECLINE ORDER] Inventory restoration attempt finished for Order {orderId}.");
                        }
                        else
                        {
                            inventoryRestored = true; // Không có item, coi như thành công
                        }
                    }
                    else
                    {
                        inventoryRestored = true; // Trạng thái Pending không cần khôi phục kho
                    }


                    // *** CHUẨN BỊ HỦY ĐƠN GHTK (NẾU CÓ) ***
                    if (originalStatus == "Shipping" && !string.IsNullOrEmpty(order.ShippingCode))
                    {
                        shippingCodeToCancel = order.ShippingCode; // Lưu lại để gọi API hủy sau transaction
                        order.ShippingCode = null; // Xóa mã GHTK khỏi Order
                        order.TransportUnit = null; // Xóa đơn vị vận chuyển
                    }

                    // *** CẬP NHẬT TRẠNG THÁI ORDER -> DECLINED ***
                    order.OrderStatus = "Declined";
                    order.UpdatedDate = DateTime.UtcNow;
                    // Không cần UpdateAsync nếu repo dùng DbContext tracking
                    await _orderRepo.SaveChangesAsync(); // Lưu trạng thái Order + ShippingCode=null (nếu có)

                    scope.Complete(); // Commit transaction
                    Console.WriteLine($"[DECLINE ORDER COMPLETED] Order {orderId} status set to Declined.");

                } // Kết thúc TransactionScope
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DECLINE ORDER FAILED] Error during DeclineOrderAsync for Order {orderId}: {ex.Message}");
                // Transaction tự động rollback
                return new ApiResponse<bool> { Success = false, Message = $"Lỗi hệ thống khi từ chối đơn hàng: {ex.Message}" };
            }

            // *** GỌI API HỦY GHTK (SAU KHI DB ĐÃ COMMIT, NẾU CÓ) ***
            if (shippingCodeToCancel != null)
            {
                Console.WriteLine($"[OrderService] Attempting to cancel GHTK shipment after declining Order {orderId}, ShippingCode {shippingCodeToCancel}");
                try
                {
                    // Chạy hủy GHTK trong nền để không block request admin
                    _backgroundJobService.Enqueue<IGhtkService>(
                       service => service.CancelShipmentAsync(shippingCodeToCancel)
                    );
                    Console.WriteLine($"[OrderService] Enqueued GHTK cancellation job for {shippingCodeToCancel}.");
                    // bool cancelSuccess = await _ghtkService.CancelShipmentAsync(shippingCodeToCancel); // Nếu muốn chạy trực tiếp
                    // if (!cancelSuccess) { ... log lỗi ... } else { ... log success ... }
                }
                catch (Exception ghtkEx)
                {
                    // Log lỗi nghiêm trọng khi *lên lịch* hủy GHTK
                    Console.WriteLine($"[GHTK CANCEL ENQUEUE EXCEPTION - Action Required] Order {orderId}: Error enqueueing GHTK cancel job for {shippingCodeToCancel}. Error: {ghtkEx.Message}. Please check manually.");
                }
            }

            // *** GỬI EMAIL THÔNG BÁO TỪ CHỐI (Sau Transaction) ***
            if (userForEmail != null)
            {
                try
                {
                    string subject = $"EcoWrap: Thông báo về đơn hàng #{orderId}";
                    string body = $@"<html><body>
                                    <p>Chào {userForEmail.FullName},</p>
                                    <p>Chúng tôi rất tiếc phải thông báo đơn hàng <strong>#{orderId}</strong> của bạn đã bị từ chối/hủy bởi quản trị viên.</p>
                                    {(inventoryRestored ? "<p>Nếu đơn hàng đã được xử lý trước đó, số lượng sản phẩm có thể đã được hoàn lại kho.</p>" : "<p>Lưu ý: Có thể đã xảy ra lỗi khi hoàn lại số lượng sản phẩm vào kho (nếu có), vui lòng liên hệ hỗ trợ nếu cần.</p>")}
                                    <p>Nếu bạn có thắc mắc, vui lòng liên hệ chúng tôi.</p>
                                    <p>Cảm ơn,</p><p>Đội ngũ EcoWrap.</p>
                                    </body></html>";

                    _backgroundJobService.Enqueue<IEmailService>(
                        service => service.SendEmailAsync(userForEmail.Email, subject, body)
                    );
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"[Hangfire Error] Failed to enqueue Decline Email for OrderId {orderId}: {emailEx.Message}");
                }
            }

            return new ApiResponse<bool> { Success = true, Message = "Đơn hàng đã được từ chối/hủy thành công." };
        }

        // --- GET ALL ORDERS (ADMIN) ---
        public async Task<ApiResponse<List<OrderResponse>>> GetAllOrdersAdminAsync() // <= Implement Yêu cầu 1
        {
            Console.WriteLine("[OrderService] Getting all orders for Admin...");
            try
            {
                // Gọi Repo để lấy tất cả Orders, nên Include Customer.User và sắp xếp
                var orders = await _orderRepo.GetAllAdminAsync(); // Cần Repo có hàm này

                var orderResponses = orders
                    .Select(order => MapOrderToOrderResponse(order)) // Dùng helper map cơ bản
                    .ToList();

                Console.WriteLine($"[OrderService] Retrieved {orderResponses.Count} orders.");
                return new ApiResponse<List<OrderResponse>>
                {
                    Success = true,
                    Data = orderResponses,
                    Message = $"Lấy thành công {orderResponses.Count} đơn hàng."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetAllOrdersAdminAsync EXCEPTION]: {ex.ToString()}");
                return new ApiResponse<List<OrderResponse>> { Success = false, Message = "Lỗi hệ thống khi lấy tất cả đơn hàng." };
            }
        }

        // --- GET ORDERS BY USER --- (Hàm cần thiết cho Customer)
        public async Task<ApiResponse<List<CustomerOrderDetailResponse>>> GetAllOrdersByUserAsync(int userId)
        {
            Console.WriteLine($"[OrderService] Getting detailed orders for User {userId}...");
            try
            {
                var customer = await _customerRepo.GetByUserIdAsync(userId);
                if (customer == null)
                {
                    // Return empty list if customer not found
                    return new ApiResponse<List<CustomerOrderDetailResponse>>
                    {
                        Success = true,
                        Data = new List<CustomerOrderDetailResponse>(),
                        Message = "Không tìm thấy thông tin khách hàng."
                    };
                }

                // *** Use the REPO METHOD that INCLUDES details ***
                // ASSUMPTION: You have GetAllWithDetailsByCustomerIdAsync in IOrderRepository/OrderRepository
                var orders = await _orderRepo.GetAllWithDetailsByCustomerIdAsync(customer.CustomerId);

                // *** MAP to the CUSTOMER-specific DTO ***
                var orderDetailResponses = orders.Select(order => new CustomerOrderDetailResponse
                {
                    // Map Order fields
                    OrderId = order.OrderId,
                    OrderStatus = order.OrderStatus,
                    CreatedDate = order.CreatedDate.GetValueOrDefault(),
                    UpdatedDate = order.UpdatedDate,
                    TotalPrice = order.TotalPrice.GetValueOrDefault(),
                    TotalAmount = order.TotalAmount.GetValueOrDefault(),
                    ShippingAddress = order.Address,
                    ShippingWard = order.Ward,
                    ShippingDistrict = order.District,
                    ShippingProvince = order.Province,
                    ShippingHamlet = order.Hamlet,
                    TransportUnit = order.TransportUnit,
                    ShippingCode = order.ShippingCode,

                    // Map Items WITHOUT Seller info
                    Items = order.CartItems?.Select(item => new CustomerOrderItemDetail
                    {
                        CartItemId = item.CartItemId,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.ProductName,
                        ProductImageUrl = item.Product?.ImageFile,
                        Quantity = item.Quantity,
                        PricePerItem = (item.Quantity > 0) ? (item.CartItemPrice / item.Quantity) : 0,
                        DesignFile = item.Product?.DesignFile
                    }).ToList() ?? new List<CustomerOrderItemDetail>() // Handle null CartItems
                }).ToList(); // Convert the result to a List

                Console.WriteLine($"[OrderService] Retrieved {orderDetailResponses.Count} detailed orders for User {userId}.");
                return new ApiResponse<List<CustomerOrderDetailResponse>>
                {
                    Success = true,
                    Data = orderDetailResponses,
                    Message = $"Tìm thấy {orderDetailResponses.Count} đơn hàng."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetAllOrdersByUserAsync EXCEPTION] UserId {userId}: {ex.ToString()}");
                return new ApiResponse<List<CustomerOrderDetailResponse>>
                { // Match return type on error
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy danh sách đơn hàng."
                    // Data will be null
                };
            }
        }

        // --- GET ORDER BY ID (CUSTOMER / BASIC VIEW) ---
        public async Task<ApiResponse<OrderDetailResponse>> GetOrderByIdAsync(int orderId, int userId) // <= Implement Yêu cầu 2 (view thường)
        {
            Console.WriteLine($"[OrderService] Getting Order {orderId} details for User {userId}...");
            try
            {
                // Lấy Order kèm Customer.User, Items.Product
                var order = await _orderRepo.GetByIdWithDetailsAsync(orderId); // Cần Repo có hàm này
                if (order == null)
                {
                    return new ApiResponse<OrderDetailResponse> { Success = false, Message = $"Không tìm thấy đơn hàng ID: {orderId}" };
                }

                // Kiểm tra quyền
                string userRole = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? "";
                bool isOwner = order.Customer?.UserId == userId;
                bool isAdmin = userRole == "Admin";
                // bool isSellerAllowed = ... // Logic kiểm tra Seller nếu cần

                if (!isOwner && !isAdmin /* && !isSellerAllowed */)
                {
                    Console.WriteLine($"[AUTH REJECT] GetOrderByIdAsync: User {userId} (Role: {userRole}) accessing Order {orderId} owned by User {order.Customer?.UserId}.");
                    return new ApiResponse<OrderDetailResponse> { Success = false, Message = "Bạn không có quyền xem đơn hàng này." };
                }

                var user = order.Customer?.User ?? await _userRepo.GetByIdAsync(order.Customer?.UserId ?? 0);

                // Map sang DTO chi tiết thông thường
                var response = new OrderDetailResponse
                {
                    OrderId = order.OrderId,
                    OrderStatus = order.OrderStatus,
                    CreatedDate = order.CreatedDate.GetValueOrDefault(),
                    UpdatedDate = order.UpdatedDate,
                    TotalPrice = order.TotalPrice.GetValueOrDefault(),
                    TotalAmount = order.TotalAmount.GetValueOrDefault(),
                    CustomerId = order.CustomerId,
                    CustomerName = user?.FullName,
                    CustomerEmail = user?.Email,
                    CustomerPhone = user?.PhoneNumber,
                    ShippingAddress = order.Address,
                    ShippingWard = order.Ward,
                    ShippingDistrict = order.District,
                    ShippingProvince = order.Province,
                    ShippingHamlet = order.Hamlet,
                    TransportUnit = order.TransportUnit,
                    ShippingCode = order.ShippingCode,
                    Items = order.CartItems?.Select(item => new OrderItemDetailResponse
                    {
                        CartItemId = item.CartItemId,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.ProductName,
                        Quantity = item.Quantity,
                        PricePerItem = (item.Quantity > 0) ? (item.CartItemPrice / item.Quantity) : 0, // Tính giá đơn vị
                        //TotalItemPrice = item.CartItemPrice, // Dùng TotalItemPrice tính sẵn của DTO
                        DesignFile = item.DesignFile
                    }).ToList() ?? new List<OrderItemDetailResponse>()
                };

                return new ApiResponse<OrderDetailResponse> { Success = true, Data = response };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrderByIdAsync EXCEPTION] OrderId {orderId}, UserId {userId}: {ex.ToString()}");
                return new ApiResponse<OrderDetailResponse> { Success = false, Message = "Lỗi hệ thống khi lấy chi tiết đơn hàng." };
            }
        }

        // --- GET ORDER BY ID (FOR ADMIN WITH SELLER INFO) ---
        public async Task<ApiResponse<AdminOrderDetailResponse>> GetOrderByIdForAdminAsync(int orderId)
        {
            Console.WriteLine($"[OrderService] Getting Order {orderId} details for Admin...");
            try
            {
                // Lấy Order kèm Customer/User, CartItems -> Product -> Seller -> User
                var order = await _dbContext.Orders
                    .Where(o => o.OrderId == orderId)
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Include(o => o.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Seller)
                                .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return new ApiResponse<AdminOrderDetailResponse>
                    {
                        Success = false,
                        Message = $"Không tìm thấy đơn hàng ID: {orderId}"
                    };
                }

                // Map sang DTO chi tiết cho Admin
                var response = MapOrderToAdminDetail(order);
                if (response == null)
                {
                    throw new InvalidOperationException($"Lỗi khi ánh xạ dữ liệu chi tiết cho đơn hàng {orderId}.");
                }

                return new ApiResponse<AdminOrderDetailResponse>
                {
                    Success = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrderByIdForAdminAsync EXCEPTION] OrderId {orderId}: {ex}");
                return new ApiResponse<AdminOrderDetailResponse>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy chi tiết đơn hàng cho Admin."
                };
            }
        }

        // --- GET ORDERS BY STATUS (ADMIN) ---
        public async Task<ApiResponse<List<AdminOrderDetailResponse>>> GetOrdersByStatusAdminAsync(string status)
        {
            Console.WriteLine($"[OrderService] Getting orders with status '{status}' for Admin...");

            // Validate status
            var allowedStatuses = new[] { "Pending", "Packaging", "Shipping", "Completed", "Canceled", "Declined" };
            if (string.IsNullOrWhiteSpace(status) || !allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return new ApiResponse<List<AdminOrderDetailResponse>>
                {
                    Success = false,
                    Message = $"Trạng thái '{status}' không hợp lệ."
                };
            }

            try
            {
                // Lấy orders từ DB, include đầy đủ Customer/User và CartItems -> Product -> Seller -> User
                var orders = await _dbContext.Orders
                    .Where(o => o.OrderStatus == status)
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Include(o => o.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Seller)
                                .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

                var orderResponses = orders
                    .Select(order => MapOrderToAdminDetail(order))
                    .ToList();

                Console.WriteLine($"[OrderService] Retrieved {orderResponses.Count} orders with status '{status}'.");

                return new ApiResponse<List<AdminOrderDetailResponse>>
                {
                    Success = true,
                    Data = orderResponses,
                    Message = $"Lấy thành công {orderResponses.Count} đơn hàng có trạng thái '{status}'."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetOrdersByStatusAdminAsync EXCEPTION] Status {status}: {ex}");
                return new ApiResponse<List<AdminOrderDetailResponse>>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy đơn hàng theo trạng thái."
                };
            }
        }

        // --- HÀM HELPER ---
        private OrderResponse MapOrderToOrderResponse(Order order)
        {
            // Kiểm tra null cơ bản
            if (order == null)
            {
                // Có thể trả về null hoặc một đối tượng rỗng tùy logic xử lý lỗi của bạn
                return new OrderResponse();
            }

            return new OrderResponse
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                // Cân nhắc thêm CustomerName nếu cần và repo đã include:
                // CustomerName = order.Customer?.User?.FullName,
                TotalAmount = order.TotalAmount.GetValueOrDefault(), // Dùng GetValueOrDefault để xử lý Nullable<int>
                TotalPrice = order.TotalPrice.GetValueOrDefault(),   // Dùng GetValueOrDefault để xử lý Nullable<decimal>
                Address = order.Address,
                Province = order.Province,
                District = order.District,
                Ward = order.Ward,
                Hamlet = order.Hamlet,
                TransportUnit = order.TransportUnit,
                ShippingCode = order.ShippingCode,
                OrderStatus = order.OrderStatus, // Giả sử OrderStatus không null trong DB/Entity
                CreatedDate = order.CreatedDate.GetValueOrDefault(), // Dùng GetValueOrDefault để xử lý Nullable<DateTime>
                UpdatedDate = order.UpdatedDate // UpdatedDate là Nullable<DateTime>
            };
        }

        private AdminOrderDetailResponse MapOrderToAdminDetail(Order order)
        {
            // Kiểm tra null cơ bản
            if (order == null)
            {
                return null; // Trả về null nếu order không tồn tại
            }

            // Map thông tin cơ bản của Order
            var response = new AdminOrderDetailResponse
            {
                OrderId = order.OrderId,
                OrderStatus = order.OrderStatus ?? "N/A", // Cung cấp giá trị mặc định nếu có thể null
                CreatedDate = order.CreatedDate.GetValueOrDefault(),
                UpdatedDate = order.UpdatedDate,
                TotalPrice = order.TotalPrice.GetValueOrDefault(),
                TotalAmount = order.TotalAmount.GetValueOrDefault(),

                // Map thông tin Khách hàng (từ navigation properties đã Include)
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.User?.FullName, // Cần Include(o => o.Customer.User)
                CustomerEmail = order.Customer?.User?.Email,
                CustomerPhone = order.Customer?.User?.PhoneNumber,

                // Map địa chỉ giao hàng
                ShippingAddress = order.Address,
                ShippingWard = order.Ward,
                ShippingDistrict = order.District,
                ShippingProvince = order.Province,
                ShippingHamlet = order.Hamlet,

                // Map thông tin vận chuyển
                TransportUnit = order.TransportUnit,
                ShippingCode = order.ShippingCode,

                // Map danh sách Items kèm thông tin Seller
                Items = order.CartItems? // Kiểm tra CartItems có null không
                           .Select(item => new AdminOrderItemDetail
                           {
                               CartItemId = item.CartItemId,
                               ProductId = item.ProductId,
                               ProductName = item.Product?.ProductName, // Cần Include(o=>o.CartItems.Product)
                               Quantity = item.Quantity,
                               // Tính giá mỗi sản phẩm một cách an toàn
                               PricePerItem = (item.Quantity > 0) ? (item.CartItemPrice / item.Quantity) : 0,
                               // TotalItemPrice sẽ được DTO tự tính
                               DesignFile = item.DesignFile,

                               // Thông tin Seller (từ Product -> Seller -> User)
                               // Cần Include(...Product.Seller.User)
                               SellerId = item.Product?.SellerId ?? 0,
                               SellerCompanyName = item.Product?.Seller?.CompanyName,
                               SellerBankAccountNumber = item.Product?.Seller?.BankAccountNumber,
                               SellerBankName = item.Product?.Seller?.BankName,
                               SellerAccountHolderName = item.Product?.Seller?.AccountHolderName,
                               SellerBankBranch = item.Product?.Seller?.BankBranch,
                               SellerUserEmail = item.Product?.Seller?.User?.Email,
                               SellerUserPhone = item.Product?.Seller?.User?.PhoneNumber
                           })
                           .ToList() // Chuyển kết quả Select thành List
                        ?? new List<AdminOrderItemDetail>() // Trả về list rỗng nếu CartItems là null
            };

            return response;
        }
    }
}

// --- Hàm Helper ---
//private OrderResponse MapOrderToOrderResponse(Order order)
//{
//    return new OrderResponse
//    {
//        OrderId = order.OrderId,
//        CustomerId = order.CustomerId,
//        TotalAmount = order.TotalAmount.GetValueOrDefault(),
//        TotalPrice = order.TotalPrice.GetValueOrDefault(),
//        Address = order.Address,
//        // Thêm các trường địa chỉ chi tiết nếu cần
//        TransportUnit = order.TransportUnit,
//        ShippingCode = order.ShippingCode,
//        OrderStatus = order.OrderStatus,
//        CreatedDate = order.CreatedDate.GetValueOrDefault()
//    };
//}