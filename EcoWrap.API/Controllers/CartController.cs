using EcoWrap.BLL.IServices;
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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Helper: Lấy userId từ JWT
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new InvalidOperationException("Không thể xác định người dùng từ token.");
            return int.Parse(userIdClaim);
        }

        // -------------------- [POST] /api/cart --------------------
        [HttpPost]
        public async Task<IActionResult> CreateCartAsync()
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _cartService.CreateCartAsync(userId);
                return Ok(result);
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

        // -------------------- [GET] /api/cart --------------------
        [HttpGet]
        public async Task<IActionResult> GetAllCartsAsync()
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _cartService.GetAllCartsAsync(userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        // -------------------- [GET] /api/cart/{cartId} --------------------
        [HttpGet("{cartId:int}")]
        public async Task<IActionResult> GetCartByIdAsync(int cartId)
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _cartService.GetCartByIdAsync(userId, cartId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        // -------------------- [DELETE] /api/cart/{cartId} --------------------
        [HttpDelete("{cartId:int}")]
        public async Task<IActionResult> DeleteCartAsync(int cartId)
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _cartService.DeleteCartAsync(userId, cartId);
                return Ok(result);
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