// File: Models/Ghtk/GhtkRequest.cs (Chứa cấu trúc chính)
using System.Text.Json.Serialization;
using System.Collections.Generic;

// Cấu trúc JSON gửi đi cho GHTK: { "order": { ... }, "products": [ ... ] }
public class GhtkOrderWrapper
{
    [JsonPropertyName("order")] public GhtkOrder Order { get; set; }
    [JsonPropertyName("products")] public List<GhtkProduct> Products { get; set; }
}

public class GhtkOrder
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } // Tên người nhận
    [JsonPropertyName("tel")] public string Tel { get; set; } // SĐT người nhận
    [JsonPropertyName("address")] public string Address { get; set; } // Địa chỉ nhận chi tiết
    [JsonPropertyName("province")] public string Province { get; set; } // Tỉnh/TP người nhận
    [JsonPropertyName("district")] public string District { get; set; } // Quận/Huyện người nhận
    [JsonPropertyName("ward")] public string Ward { get; set; } // Phường/Xã người nhận
    [JsonPropertyName("hamlet")] public string Hamlet { get; set; }
    [JsonPropertyName("pick_money")] public int PickMoney { get; set; } // Số tiền COD (Total Price)
    [JsonPropertyName("value")] public int Value { get; set; }

    // Thông tin lấy hàng (SHOP)
    [JsonPropertyName("pick_name")] public string PickName { get; set; }
    [JsonPropertyName("pick_address")] public string PickAddress { get; set; }
    [JsonPropertyName("pick_province")] public string PickProvince { get; set; }
    [JsonPropertyName("pick_district")] public string PickDistrict { get; set; }
    [JsonPropertyName("pick_tel")] public string PickTel { get; set; }
    [JsonPropertyName("weight")] public int Weight { get; set; } // Tổng trọng lượng (gram)
}

public class GhtkProduct
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("weight")] public int Weight { get; set; } // gram
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("product_code")] public string ProductCode { get; set; }
}