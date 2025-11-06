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
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [Authorize(Roles = "Seller")]
        [HttpPost("")]
        public async Task<IActionResult> Create([FromBody] CreatInventoryRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized("UserId không hợp lệ.");

                var inventory = await _inventoryService.CreateInventoryAsync(userId, request);
                return Ok(inventory);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var inventories = await _inventoryService.GetAllAsync();
            return Ok(inventories);
        }

        [Authorize(Roles = "Admin, Seller")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var inventory = await _inventoryService.GetByIdAsync(id);
            if (inventory == null) return NotFound("Inventory không tồn tại.");
            return Ok(inventory);
        }

        [Authorize(Roles = "Admin, Seller")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyInventory()
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("UserId không hợp lệ.");
                }

                var inventories = await _inventoryService.GetMyInventoryAsync(userId);
                return Ok(inventories);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nội bộ server khi lấy tồn kho.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Seller")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInventoryRequest request)
        {
            try
            {
                var inventory = await _inventoryService.UpdateInventoryAsync(id, request);
                return Ok(inventory);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Seller")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _inventoryService.DeleteInventoryAsync(id);
            if (!success) return NotFound("Inventory không tồn tại.");
            return Ok(new { message = "Xóa Inventory thành công." });
        }
    }
}