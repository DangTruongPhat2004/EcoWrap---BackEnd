using System;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;

        public SellerController(ISellerService sellerService)
        {
            _sellerService = sellerService ?? throw new ArgumentNullException(nameof(sellerService));
        }

        // ------------------------ [GET ALL SELLERS] ------------------------
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllSellers()
        {
            var sellers = await _sellerService.GetAllAsync();
            return Ok(sellers);
        }

        // ------------------------ [GET SELLER BY ID] ------------------------
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSellerById(int id)
        {
            var seller = await _sellerService.GetBySellerIdAsync(id);
            if (seller == null)
                return NotFound(new { Message = "Không tìm thấy người bán." });

            return Ok(seller);
        }

        [HttpGet("me")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetMySellerInfo()
        {
            try
            {
                // Lấy userId từ JWT claim
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { Message = "Không xác định được người dùng đang đăng nhập." });

                int userId = int.Parse(userIdClaim);

                var seller = await _sellerService.GetCurrentSellerAsync(userId);
                if (seller == null)
                    return NotFound(new { Message = "Không tìm thấy thông tin người bán." });

                return Ok(new
                {
                    Message = "Lấy thông tin người bán thành công.",
                    Data = seller
                });
            }
            catch (InvalidOperationException ex)
            {
                // Xử lý lỗi nghiệp vụ: không có thông tin người bán
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi hệ thống
                return StatusCode(500, new { Message = "Đã xảy ra lỗi không mong muốn.", Detail = ex.Message });
            }
        }

        // ------------------------ [UPDATE SELLER] ------------------------
        [HttpPut]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateSeller([FromForm] UpdateSellerRequest request)
        {
            if (request == null)
                return BadRequest(new { Message = "Dữ liệu cập nhật không hợp lệ." });

            // Lấy userId từ JWT
            var userIdClaim = User.FindFirst("id")?.Value; // vì token của bạn dùng "id"
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = "Không xác định được người dùng." });

            int userId = int.Parse(userIdClaim);

            var updated = await _sellerService.UpdateSellerAsync(userId, request);
            if (!updated)
                return NotFound(new { Message = "Không tìm thấy người bán để cập nhật." });

            return Ok(new { Message = "Cập nhật thông tin người bán thành công." });
        }

        // ------------------------ [DELETE SELLER] ------------------------
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> DeleteSeller(int id)
        {
            var deleted = await _sellerService.DeleteSellerAsync(id);
            if (!deleted)
                return NotFound(new { Message = "Không tìm thấy người bán để xóa." });

            return Ok(new { Message = "Xóa người bán thành công." });
        }
    }
}