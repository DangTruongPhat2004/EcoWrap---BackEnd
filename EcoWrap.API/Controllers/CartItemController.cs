using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartItemController : ControllerBase
    {
        private readonly ICartItemService _cartItemService;

        public CartItemController(ICartItemService cartItemService)
        {
            _cartItemService = cartItemService;
        }

        // Lấy userId từ JWT Claims
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new InvalidOperationException("Không tìm thấy thông tin người dùng.");
            return int.Parse(userIdClaim);
        }

        // -------------------- [POST] /api/cartitem --------------------
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] CreateCartItemRequest request)
        {
            try
            {
                int userId = GetUserId();
                var result = await _cartItemService.CreateAsync(request, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        // -------------------- [PUT] /api/cartitem/{cartItemId} --------------------
        [HttpPut("{cartItemId:int}")]
        public async Task<IActionResult> UpdateAsync(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                int userId = GetUserId();
                var result = await _cartItemService.UpdateAsync(cartItemId, request, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        // -------------------- [DELETE] /api/cartitem/{cartItemId} --------------------
        [HttpDelete("{cartItemId:int}")]
        public async Task<IActionResult> DeleteAsync(int cartItemId)
        {
            try
            {
                int userId = GetUserId();
                var success = await _cartItemService.DeleteAsync(cartItemId, userId);
                if (!success)
                    return NotFound(new { Message = "Không tìm thấy sản phẩm trong giỏ hàng để xóa." });

                return Ok(new { Message = "Đã xóa sản phẩm khỏi giỏ hàng thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }
        }
    }
}