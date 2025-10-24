using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Response;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using EcoWrap.Models.Ghtk;

namespace EcoWrap.BLL.Services
{
    public class GhtkService : IGhtkService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _ghtkToken;
        private readonly string _ghtkApiUrl;
        private readonly IUserService _userService;

        // Đã thay thế ICartItemRepository bằng ICartItemService (Service Layer)
        private readonly ICartItemService _cartItemService;
        private readonly IProductService _productService;
        private readonly IAddressService _addressService;

        public GhtkService(
            HttpClient httpClient,
            IConfiguration configuration,
            ICartItemService cartItemService, // Đã thay đổi
            IProductService productService,
            IAddressService addressService,
            IUserService userService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _ghtkToken = configuration["GHTK:Token"]
                ?? throw new InvalidOperationException("Missing GHTK:Token configuration.");
            _ghtkApiUrl = configuration["GHTK:ApiUrl"]
                ?? throw new InvalidOperationException("Missing GHTK:ApiUrl configuration.");

            _cartItemService = cartItemService; // Đã thay đổi
            _productService = productService;
            _addressService = addressService;

            _httpClient.DefaultRequestHeaders.Add("Token", _ghtkToken);
            _httpClient.BaseAddress = new Uri(_ghtkApiUrl);
            _userService = userService;
        }

        public async Task<GhtkResponse> CreateShipmentAsync(Order order, User user)
        { 
            // 2. Chuẩn bị danh sách sản phẩm (Products) và tính tổng trọng lượng
            var cartItems = await _cartItemService.GetCartItemsByOrderIdAsync(order.OrderId);

            var ghtkProducts = new List<GhtkProduct>();
            decimal totalWeightGram = 0;

            foreach (var item in cartItems)
            {
                // Lấy chi tiết Product để có tên và trọng lượng
                // Giả định productDetail có thuộc tính WeightGram (int)
                var productDetail = await _productService.GetProductDetailAsync(item.ProductId);
                decimal productWeightKg = (decimal)productDetail.WeightGram / 1000M;
                // Thêm sản phẩm vào Payload
                ghtkProducts.Add(new GhtkProduct
                {
                    Name = productDetail.ProductName,
                    Weight = (int)Math.Ceiling((decimal)productDetail.WeightGram / 1000M),
                    Quantity = item.Quantity,
                    ProductCode = item.ProductId.ToString() // GHTK yêu cầu mã sản phẩm là string
                });

                // Tính tổng trọng lượng
                totalWeightGram += (decimal)productDetail.WeightGram * item.Quantity;
            }

            decimal totalWeightKg = totalWeightGram / 1000M;

            // 3. Chuẩn bị Payload (Order)
            var ghtkOrder = new GhtkOrder
            {
                Id = order.OrderId.ToString(),
                Name = user.FullName,
                Tel = user.PhoneNumber,
                Address = order.Address,
                Province = order.Province ?? "Khác",
                District = order.District ?? "Khác",
                Ward = order.Ward ?? "Khác",
                Hamlet = order.Hamlet ?? "Khác",
                PickMoney = (int)(order.TotalPrice ?? 0),
                Value = (int)(order.TotalPrice ?? 0),

                // Thông tin SHOP (Lấy từ cấu hình)
                PickName = _configuration["GHTK:ShopName"],
                PickAddress = _configuration["GHTK:ShopPickAddress"],
                PickProvince = _configuration["GHTK:ShopPickProvince"],
                PickDistrict = _configuration["GHTK:ShopPickDistrict"],
                PickTel = _configuration["GHTK:ShopPickTel"],
                Weight = (int)Math.Round(totalWeightKg)
            };

            var orderWrapper = new GhtkOrderWrapper { Order = ghtkOrder, Products = ghtkProducts };
            // Tự serialize với cấu hình để kiểm tra
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonPayload = JsonSerializer.Serialize(orderWrapper, options);

            // LOG RA CONSOLE để kiểm tra tên thuộc tính JSON
            Console.WriteLine($"[DEBUG GHTK PAYLOAD] JSON: {jsonPayload}");

            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 4. Gọi API
            var response = await _httpClient.PostAsync("services/shipment/order", httpContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            // LOG PHẢN HỒI GHTK
            Console.WriteLine($"[DEBUG GHTK RESPONSE] HTTP Status: {response.StatusCode}. JSON: {jsonResponse}");

            // 5. Xử lý Response
            try
            {
                var apiResult = JsonSerializer.Deserialize<GhtkApiResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response.IsSuccessStatusCode && apiResult != null && apiResult.Success && apiResult.Order != null)
                {
                    return new GhtkResponse
                    {
                        Success = true,
                        TransportUnit = "GHTK",
                        ShippingCode = apiResult.Order.Label,
                        Message = apiResult.Message ?? "Tạo đơn GHTK thành công."
                    };
                }
                else
                {
                    // Lỗi từ GHTK (Success: false) hoặc lỗi HTTP khác
                    return new GhtkResponse
                    {
                        Success = false,
                        TransportUnit = "GHTK",
                        ShippingCode = null,
                        Message = apiResult?.Message ?? $"Lỗi không xác định hoặc lỗi HTTP: {response.StatusCode}."
                    };
                }
            }
            catch (JsonException ex)
            {
                // Lỗi Parsing JSON
                throw new Exception($"Lỗi khi phân tích JSON từ GHTK: {ex.Message}. Response: {jsonResponse}", ex);
            }
            catch (Exception ex)
            {
                // Lỗi chung (ví dụ: Timeout, Network)
                return new GhtkResponse
                {
                    Success = false,
                    TransportUnit = "GHTK",
                    ShippingCode = null,
                    Message = $"Lỗi kết nối hoặc xử lý GHTK API: {ex.Message}"
                };
            }
        }
        // --- HÀM HỦY ĐƠN GHTK (ĐÃ XÁC NHẬN VỚI TÀI LIỆU) ---
        public async Task<bool> CancelShipmentAsync(string shippingCode)
        {
            if (string.IsNullOrWhiteSpace(shippingCode))
            {
                Console.WriteLine("[GHTK Cancel] Error: Shipping code is empty.");
                return false;
            }

            // Endpoint hủy đơn theo tài liệu GHTK
            var cancelEndpoint = $"/services/shipment/cancel/{shippingCode.Trim()}";

            Console.WriteLine($"[GHTK Cancel] Attempting to cancel shipment: {shippingCode} via POST {cancelEndpoint}");

            try
            {
                // GHTK dùng POST, không cần body
                var response = await _httpClient.PostAsync(cancelEndpoint, null);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[GHTK Cancel] Response Status for {shippingCode}: {response.StatusCode}");
                Console.WriteLine($"[GHTK Cancel] Response Body for {shippingCode}: {jsonResponse}");

                if (response.IsSuccessStatusCode)
                {
                    // Parse JSON và kiểm tra "success": true
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                    {
                        Console.WriteLine($"[GHTK Cancel] Successfully canceled shipment: {shippingCode}");
                        return true; // Hủy thành công
                    }
                    else
                    {
                        // GHTK trả về 2xx nhưng success=false
                        string errorMessage = doc.RootElement.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown GHTK cancellation error.";
                        Console.WriteLine($"[GHTK Cancel] Failed (API success=false) for {shippingCode}: {errorMessage}");
                        return false;
                    }
                }
                else // Lỗi HTTP (4xx, 5xx)
                {
                    Console.WriteLine($"[GHTK Cancel] HTTP Error for {shippingCode}: {response.StatusCode}. Response: {jsonResponse}");
                    return false;
                }
            }
            // Bắt các lỗi khác
            catch (Exception ex)
            {
                Console.WriteLine($"[GHTK Cancel EXCEPTION] Error canceling {shippingCode}: {ex.Message}");
                return false;
            }
        }
    }
}