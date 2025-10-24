// File: BLL/Services/AddressService.cs
using EcoWrap.BLL.IServices;
using EcoWrap.Models.DTO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EcoWrap.BLL.Services
{
    public class AddressService : IAddressService
    {
        // Danh sách Tỉnh/Thành phố chuẩn để kiểm tra (THAY THẾ BẰNG DB DỮ LIỆU THỰC TẾ!)
        private readonly string[] _validProvinces = { "TP Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng", "Tỉnh Đồng Nai", "Tỉnh Bình Dương", /* ... */ };

        public Task<AddressParseResult> ParseAddressAsync(string rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress))
            {
                throw new ArgumentException("Địa chỉ không được để trống.", nameof(rawAddress));
            }

            // Tách chuỗi địa chỉ dựa trên dấu phẩy
            var parts = rawAddress.Split(',')
                                  .Select(p => p.Trim())
                                  .Where(p => !string.IsNullOrEmpty(p))
                                  .ToList();

            if (parts.Count < 3)
            {
                // Cần tối thiểu [Phường/Xã], [Quận/Huyện], [Tỉnh/TP]
                throw new InvalidOperationException("Địa chỉ phải bao gồm ít nhất Phường/Xã, Quận/Huyện và Tỉnh/TP.");
            }

            // --- GIẢI PHÁP TÁCH ĐỊA CHỈ ĐƠN GIẢN (Cần chuẩn hóa thủ công) ---

            // 1. Lấy Tỉnh/TP (Thành phần cuối cùng)
            string province = parts.Last();

            // Kiểm tra tính hợp lệ cơ bản
            if (!_validProvinces.Any(p => province.Contains(p)))
            {
                // Gán lại tên chuẩn GHTK nếu cần
            }

            // 2. Lấy Quận/Huyện (Thành phần kế cuối)
            string district = parts[parts.Count - 2];

            // 3. Lấy Phường/Xã (Thành phần thứ 3 từ cuối)
            string ward = parts[parts.Count - 3];

            // 4. Lấy Địa chỉ chi tiết (Các thành phần còn lại, bao gồm Số nhà, đường)
            string streetAddress = string.Join(", ", parts.Take(parts.Count - 3));

            return Task.FromResult(new AddressParseResult
            {
                Province = province,
                District = district,
                Ward = ward,
                StreetAddress = streetAddress
            });
        }
    }
}