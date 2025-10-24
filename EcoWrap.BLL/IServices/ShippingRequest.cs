using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public class ShippingRequest
    {
        // === THÔNG TIN NGƯỜI GỬI (ĐỘNG) ===
        public string SenderFullname { get; set; } = null!;
        public string SenderPhone { get; set; } = null!;
        public string SenderAddress { get; set; } = null!; // Số nhà, đường
        public int SenderProvinceId { get; set; }
        public int SenderDistrictId { get; set; }
        public int SenderWardId { get; set; }

        // === THÔNG TIN NGƯỜI NHẬN (TỪ KHÁCH HÀNG) ===
        public string ReceiverFullname { get; set; } = null!;
        public string ReceiverPhone { get; set; } = null!;
        public string ReceiverAddress { get; set; } = null!; // Số nhà, đường
        public int ReceiverProvinceId { get; set; }
        public int ReceiverDistrictId { get; set; }
        public int ReceiverWardId { get; set; }

        // === THÔNG TIN GÓI HÀNG ===
        public string OrderNumber { get; set; } = null!; // Mã đơn hàng của bạn
        public decimal OrderValue { get; set; } // Tổng giá trị đơn
        public int TotalWeightGram { get; set; } // Gram
        public bool IsCod { get; set; }
        public decimal CodAmount { get; set; } // Số tiền cần thu hộ
        public string Note { get; set; } = "Cho xem hàng, không cho thử";
    }

    // ShippingResponse và IShippingService giữ nguyên
    public class ShippingResponse
    {
        public bool IsSuccess { get; set; }
        public string? ShippingCode { get; set; } // Mã vận đơn
        public string? TransportUnitName { get; set; }
        public string? ErrorMessage { get; set; }
    }
    public interface IShippingService
    {
        Task<ShippingResponse> CreateShipmentAsync(ShippingRequest request);
    }
}