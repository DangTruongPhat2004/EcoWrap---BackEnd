namespace EcoWrap.Models.Responses
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int TotalAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Hamlet { get; set; }
        public string TransportUnit { get; set; }
        public string ShippingCode { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class OrderDetailResponse
    {
        // --- Thông tin cơ bản ---
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; } // Ngày cập nhật cuối
        public decimal TotalPrice { get; set; }
        public int TotalAmount { get; set; } // Tổng số lượng sản phẩm

        // --- Thông tin khách hàng ---
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; } // Lấy từ User
        public string? CustomerEmail { get; set; } // Lấy từ User
        public string? CustomerPhone { get; set; } // Lấy từ User

        // --- Địa chỉ giao hàng ---
        public string? ShippingAddress { get; set; } // Địa chỉ cụ thể
        public string? ShippingWard { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingHamlet { get; set; } // Thôn/Xóm (nếu có)

        // --- Thông tin vận chuyển ---
        public string? TransportUnit { get; set; } // Đơn vị vận chuyển (VD: GHTK)
        public string? ShippingCode { get; set; } // Mã vận đơn

        // --- Danh sách sản phẩm ---
        public List<OrderItemDetailResponse> Items { get; set; } = new List<OrderItemDetailResponse>();
    }

    public class OrderItemDetailResponse
    {
        public int CartItemId { get; set; } // Hoặc OrderItemId nếu bạn đổi tên
        public string ProductId { get; set; } = null!;
        public string? ProductName { get; set; } // Lấy từ Product
        public string? ProductImageUrl { get; set; } // Lấy từ Product (nếu cần)
        public int Quantity { get; set; }
        public decimal PricePerItem { get; set; } // Giá tại thời điểm mua (lưu trong CartItem)
        public decimal TotalItemPrice => Quantity * PricePerItem;
        public string? DesignFile { get; set; } // Link file thiết kế (nếu có)
        // Thêm các thuộc tính sản phẩm khác nếu cần (VD: Size, Material...)
    }
}