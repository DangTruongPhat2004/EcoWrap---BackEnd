using System;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
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
        public async Task<IActionResult> GetAllSellers()
        {
            var sellers = await _sellerService.GetAllAsync();
            return Ok(sellers);
        }

        // ------------------------ [GET SELLER BY ID] ------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSellerById(int id)
        {
            var seller = await _sellerService.GetBySellerIdAsync(id);
            if (seller == null)
                return NotFound(new { Message = "Không tìm thấy người bán." });

            return Ok(seller);
        }

        // ------------------------ [UPDATE SELLER] ------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSeller(int id, [FromBody] UpdateSellerRequest request)
        {
            if (request == null)
                return BadRequest(new { Message = "Dữ liệu cập nhật không hợp lệ." });

            var updated = await _sellerService.UpdateSellerAsync(id, request);
            if (!updated)
                return NotFound(new { Message = "Không tìm thấy người bán cần cập nhật." });

            return Ok(new { Message = "Cập nhật thông tin người bán thành công." });
        }

        // ------------------------ [DELETE SELLER] ------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSeller(int id)
        {
            var deleted = await _sellerService.DeleteSellerAsync(id);
            if (!deleted)
                return NotFound(new { Message = "Không tìm thấy người bán để xóa." });

            return Ok(new { Message = "Xóa người bán thành công." });
        }
    }
}