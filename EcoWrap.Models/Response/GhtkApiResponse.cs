using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace EcoWrap.Models.Ghtk
{
    // Cần cho payload GHTK (Đóng gói Order và Products)
    public class GhtkOrderWrapper
    {
        [JsonPropertyName("order")] public GhtkOrder Order { get; set; } = null!;
        [JsonPropertyName("products")] public List<GhtkProduct> Products { get; set; } = new List<GhtkProduct>();
    }

    // Cần cho payload GHTK (Thông tin Order)
    public class GhtkOrder
    {
        [JsonPropertyName("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("name")] public string Name { get; set; } = null!;
        [JsonPropertyName("tel")] public string Tel { get; set; } = null!;
        [JsonPropertyName("address")] public string Address { get; set; } = null!;
        [JsonPropertyName("province")] public string Province { get; set; } = null!;
        [JsonPropertyName("district")] public string District { get; set; } = null!;
        [JsonPropertyName("ward")] public string Ward { get; set; } = null!;

        // BẮT BUỘC THÊM: Khắc phục lỗi 30302
        [JsonPropertyName("hamlet")] public string? Hamlet { get; set; }

        [JsonPropertyName("pick_money")] public int PickMoney { get; set; }
        [JsonPropertyName("value")] public int Value { get; set; } // THÊM Value (tương tự PickMoney)

        [JsonPropertyName("pick_name")] public string PickName { get; set; } = null!;
        [JsonPropertyName("pick_address")] public string PickAddress { get; set; } = null!;
        [JsonPropertyName("pick_province")] public string PickProvince { get; set; } = null!;
        [JsonPropertyName("pick_district")] public string PickDistrict { get; set; } = null!;
        [JsonPropertyName("pick_tel")] public string PickTel { get; set; } = null!;

        // SỬA: Chuyển từ decimal sang int (kg)
        [JsonPropertyName("weight")] public int Weight { get; set; }
    }

    // Cần cho payload GHTK (Danh sách Products)
    public class GhtkProduct
    {
        [JsonPropertyName("name")] public string Name { get; set; } = null!;
        // Weight ở đây là gram (theo GHTK Product)
        [JsonPropertyName("weight")] public int Weight { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("product_code")] public string ProductCode { get; set; } = null!;
    }

    public class GhtkOrderResponseDetail
    {
        [JsonPropertyName("partner_id")] public string PartnerId { get; set; } = null!;
        [JsonPropertyName("label")] public string Label { get; set; } = null!; // Mã vận đơn GHTK

        // BẮT BUỘC SỬA: Chuyển từ string sang int
        [JsonPropertyName("area")] public int Area { get; set; }

        [JsonPropertyName("fee")] public int Fee { get; set; }
        [JsonPropertyName("insurance_fee")] public int InsuranceFee { get; set; }

        [JsonPropertyName("estimated_pick_time")] public string? EstimatedPickTime { get; set; }
        [JsonPropertyName("estimated_deliver_time")] public string? EstimatedDeliverTime { get; set; }

        // THÊM CÁC TRƯỜNG THIẾU TỪ RESPONSE THỰC TẾ
        [JsonPropertyName("status_id")] public int StatusId { get; set; }
        [JsonPropertyName("tracking_id")] public long TrackingId { get; set; }
        [JsonPropertyName("sorting_code")] public string? SortingCode { get; set; }
        [JsonPropertyName("is_xfast")] public int IsXfast { get; set; }
        // ... (Bạn có thể thêm các trường khác nếu thấy cần)
    }

    /// <summary>
    /// Model hứng cấu trúc JSON trả về từ GHTK API
    /// </summary>
    public class GhtkApiResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("order")] public GhtkOrderResponseDetail? Order { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("warning_message")] public string? WarningMessage { get; set; } // THÊM WARNING
    }
}