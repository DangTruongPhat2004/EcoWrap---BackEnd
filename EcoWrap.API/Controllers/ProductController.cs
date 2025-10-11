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
                var result = await _productService.DeleteAsync(id);
                if (result)
                    return Ok(new { message = "Xóa sản phẩm thành công." });

                return NotFound(new { message = "Không tìm thấy sản phẩm để xóa." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Hệ thống gặp lỗi, vui lòng thử lại sau!",
                    detail = ex.Message
                });
            }
        }
    }
}