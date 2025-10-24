using System;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        //Lấy tất cả kho
        [HttpGet("")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var warehouses = await _warehouseService.GetAllAsync();
            return Ok(warehouses);
        }

        //Lấy thông tin chi tiết 1 kho
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetWarehouseById(int id)
        {
            try
            {
                var warehouse = await _warehouseService.GetByIdAsync(id);
                return Ok(warehouse);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        //Lấy tất cả kho của người bán hiện tại
        [HttpGet("my")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetMyWarehouses()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin đăng nhập."));

                var warehouses = await _warehouseService.GetWarehousesBySellerAsync(userId);
                return Ok(warehouses);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Tạo kho mới
        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng."));

                await _warehouseService.CreateWarehouseAsync(userId, request);
                return Ok(new { message = "Tạo kho hàng thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ: {ex.Message}" });
            }
        }

        //Cập nhật kho
        [HttpPut("{warehouseId}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateWarehouse(int warehouseId, [FromBody] UpdateWarehouseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng."));

                await _warehouseService.UpdateWarehouseAsync(warehouseId, userId, request);
                return Ok(new { message = "Cập nhật kho hàng thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ: {ex.Message}" });
            }
        }

        //Xoá kho
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            try
            {
                // [SỬA ĐỔI] Lấy userId từ token
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
          ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng."));

                // [SỬA ĐỔI] Gọi hàm DeleteAsync với 2 tham số
                await _warehouseService.DeleteAsync(id, userId);

                return Ok(new { message = "Xoá kho hàng thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ: {ex.Message}" });
            }
        }
    }
}