using System;
using System.Security.Claims;
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
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // -------------------- [CREATE PRODUCT] --------------------
        [HttpPost("")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Create([FromForm] CreateProductRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "UserId không hợp lệ." });

                await _productService.CreateProductAsync(userId, request);

                return Ok(new
                {
                    message = "Tạo sản phẩm thành công.",
                    success = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi trong quá trình tạo sản phẩm.",
                    detail = ex.Message
                });
            }
        }

        // -------------------- [GET ALL] --------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        // -------------------- [GET BY PRODUCT ID] --------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(product);
        }

        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetProductsBySellerId(int sellerId)
        {
            try
            {
                var products = await _productService.GetProductsBySellerIdAsync(sellerId);

                // Kiểm tra xem products có rỗng không (nếu seller không có sản phẩm)
                // Trả về Ok(products) vẫn ổn (sẽ là một mảng rỗng [])

                return Ok(products);
            }
            catch (InvalidOperationException ex) // Bắt lỗi nếu service ném ra (vd: seller không tồn tại)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Không thể lấy danh sách sản phẩm.",
                    detail = ex.Message
                });
            }
        }

        // -------------------- [GET PRODUCTS BY CURRENT SELLER] --------------------
        [HttpGet("my-products")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetProductsByCurrentSeller()
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "UserId không hợp lệ." });

                var products = await _productService.GetProductsBySellerAsync(userId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Không thể lấy danh sách sản phẩm.",
                    detail = ex.Message
                });
            }
        }

        // -------------------- [UPDATE PRODUCT] --------------------
        [HttpPut("update/{productId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Update(string productId, [FromForm] UpdateProductRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "UserId không hợp lệ." });

                await _productService.UpdateProductAsync(productId, userId, request);

                return Ok(new
                {
                    message = "Cập nhật sản phẩm thành công.",
                    success = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi cập nhật sản phẩm.",
                    detail = ex.Message
                });
            }
        }

        // -------------------- [DELETE] --------------------
        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // [MODIFY] Get userId from token
                var userIdClaim = User.FindFirst("id")?.Value; // Or ClaimTypes.NameIdentifier
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "UserId không hợp lệ." });

                // [MODIFY] Call DeleteAsync with userId
                var result = await _productService.DeleteAsync(id, userId);

                // No need to check 'result' here if the service throws exceptions on failure
                return Ok(new { message = "Xóa sản phẩm thành công." });

            }
            // [ADD] Catch specific exceptions from the service
            catch (InvalidOperationException ex) // e.g., Product not found
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex) // e.g., Not the owner
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Hệ thống gặp lỗi khi xóa sản phẩm!",
                    detail = ex.Message
                });
            }
        }

        // -------------------- [ACTIVATE PRODUCT] --------------------
        [HttpPut("{productId}/activate")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ActivateProduct(string productId)
        {
            // --- Validate Input ---
            if (string.IsNullOrWhiteSpace(productId))
            {
                return BadRequest(new { Success = false, Message = "ProductId không được để trống." });
            }

            try
            {
                // --- Get User ID ---
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { Success = false, Message = "Không thể xác định User ID từ token." });
                }
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "User ID không hợp lệ trong token." });
                }
                // --- End Get User ID ---

                await _productService.ActivateProductAsync(productId, userId);
                return Ok(new { Success = true, Message = "Sản phẩm đã được kích hoạt thành công." });

            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Không tìm thấy sản phẩm") || ex.Message.Contains("Không tìm thấy thông tin người bán"))
                {
                    return NotFound(new { Success = false, Message = ex.Message });
                }
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ActivateProduct EXCEPTION] ProductId {productId}: {ex.ToString()}");
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi kích hoạt sản phẩm." });
            }
        }

        // -------------------- [DEACTIVATE PRODUCT] --------------------
        [HttpPut("{productId}/deactivate")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeactivateProduct(string productId)
        {
            // --- Validate Input ---
            if (string.IsNullOrWhiteSpace(productId))
            {
                return BadRequest(new { Success = false, Message = "ProductId không được để trống." });
            }

            try
            {
                // --- Get User ID ---
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { Success = false, Message = "Không thể xác định User ID từ token." });
                }
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "User ID không hợp lệ trong token." });
                }
                // --- End Get User ID ---

                await _productService.DeactivateProductAsync(productId, userId);
                return Ok(new { Success = true, Message = "Sản phẩm đã ngừng kích hoạt thành công." });

            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Không tìm thấy sản phẩm") || ex.Message.Contains("Không tìm thấy thông tin người bán"))
                {
                    return NotFound(new { Success = false, Message = ex.Message });
                }
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API DeactivateProduct EXCEPTION] ProductId {productId}: {ex.ToString()}");
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi ngừng kích hoạt sản phẩm." });
            }
        }
    }
}