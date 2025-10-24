using System.ComponentModel.DataAnnotations;

namespace EcoWrap.Models.Request
{
    public class CreateOrderRequest
    {
        // Địa chỉ chi tiết (Số nhà, tên đường, thôn xóm)
        [Required(ErrorMessage = "Địa chỉ chi tiết (số nhà, đường) là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 200 ký tự.")]
        public string Address { get; set; } // Tên mới, dùng cho trường 'Address' của GHTK

        // Các cấp hành chính bắt buộc
        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc.")]
        public string District { get; set; }

        [Required(ErrorMessage = "Phường/Xã là bắt buộc.")]
        public string Ward { get; set; }

        [Required(ErrorMessage = "Ấp là bắt buộc.")]
        public string Hamlet { get; set; }
    }
}