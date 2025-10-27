using System;
using System.Collections.Generic;

namespace EcoWrap.Models.Response // Hoặc namespace phù hợp
{
    // DTO chi tiết cho Admin xem
    public class AdminOrderDetailResponse
    {
        // --- Thông tin Order ---
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public int TotalAmount { get; set; } // Tổng số lượng

        // --- Thông tin Khách hàng ---
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        // --- Địa chỉ Giao hàng ---
        public string? ShippingAddress { get; set; }
        public string? ShippingWard { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingHamlet { get; set; }

        // --- Thông tin Vận chuyển ---
        public string? TransportUnit { get; set; }
        public string? ShippingCode { get; set; }

        // --- Danh sách Items với thông tin Seller ---
        public List<AdminOrderItemDetail> Items { get; set; } = new List<AdminOrderItemDetail>();
    }

    // DTO chi tiết Item cho Admin xem (kèm Seller)
    public class AdminOrderItemDetail
    {
        public int CartItemId { get; set; }
        public string ProductId { get; set; } = null!;
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerItem { get; set; }
        public decimal TotalItemPrice => Quantity * PricePerItem;
        public string? DesignFile { get; set; }

        // --- Thông tin Seller của sản phẩm này ---
        public int SellerId { get; set; }
        public string? SellerCompanyName { get; set; }
        // Thêm các thông tin cần thiết để Admin chuyển tiền
        public string? SellerBankAccountNumber { get; set; }
        public string? SellerBankName { get; set; }
        public string? SellerAccountHolderName { get; set; }
        public string? SellerBankBranch { get; set; }
        // Có thể thêm Seller Email/Phone nếu cần liên hệ
        public string? SellerUserEmail { get; set; }
        public string? SellerUserPhone { get; set; }
    }
}