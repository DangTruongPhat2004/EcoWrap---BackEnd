namespace EcoWrap.Models.DTO
{
    public class AddressParseResult
    {
        public string Province { get; set; } // Tỉnh/Thành phố
        public string District { get; set; } // Quận/Huyện
        public string Ward { get; set; }     // Phường/Xã
        public string StreetAddress { get; set; } // Số nhà, tên đường/ấp/khu phố
    }
}