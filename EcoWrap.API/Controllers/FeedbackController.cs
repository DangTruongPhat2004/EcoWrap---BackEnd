using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using EcoWrap.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            // Không cần lấy userId ở đây
            try
            {
                // Gọi hàm service không cần userId
                var result = await _feedbackService.CreateFeedbackAsync(request);

                if (result.Success)
                {
                    return Ok(result); // Trả về 200 OK
                }

                // Service sẽ tự kiểm tra xác thực
                if (result.Message.Contains("không được xác thực") || result.Message.Contains("không tồn tại"))
                {
                    return Unauthorized(result); // 401
                }
                return BadRequest(result); // 400
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API CreateFeedback EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi gửi đánh giá." });
            }
        }

        [HttpGet("my-feedback")]
        public async Task<IActionResult> GetMyFeedback()
        {
            // Không cần lấy userId ở đây
            try
            {
                // Gọi hàm service không cần userId
                var result = await _feedbackService.GetMyFeedbackAsync();

                if (result.Success)
                {
                    return Ok(result);
                }
                // Service sẽ tự kiểm tra xác thực
                if (result.Message.Contains("không được xác thực"))
                {
                    return Unauthorized(result); // 401
                }
                // Các lỗi khác (hiếm)
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetMyFeedback EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy đánh giá của bạn." });
            }
        }
        
        [HttpPut("my-feedback/{id}")]
        public async Task<IActionResult> UpdateMyFeedback(int id, [FromBody] UpdateFeedbackRequest request)
        {
            // Không cần lấy userId ở đây
            try
            {
                // Gọi hàm service không cần userId
                var result = await _feedbackService.UpdateMyFeedbackAsync(id, request);

                if (result.Success)
                {
                    return Ok(result);
                }

                // Phân loại lỗi trả về từ Service
                if (result.Message.Contains("không được xác thực"))
                {
                    return Unauthorized(result); // 401
                }
                //if (result.Message.Contains("không có quyền"))
                //{
                //    return Forbid(result); // 403
                //}
                if (result.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(result); // 404
                }
                return BadRequest(result); // 400
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API UpdateMyFeedback EXCEPTION] FeedbackId {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi cập nhật đánh giá." });
            }
        }

        [HttpDelete("my-feedback/{id}")]
        public async Task<IActionResult> DeleteMyFeedback(int id)
        {
            // Không cần lấy userId ở đây
            try
            {
                // Gọi hàm service không cần userId
                var result = await _feedbackService.DeleteMyFeedbackAsync(id);

                if (result.Success)
                {
                    return Ok(result);
                }

                // Phân loại lỗi trả về từ Service
                if (result.Message.Contains("không được xác thực"))
                {
                    return Unauthorized(result); // 401
                }
                //if (result.Message.Contains("không có quyền"))
                //{
                //    return Forbid(result); // 403
                //}
                if (result.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(result); // 404
                }
                return BadRequest(result); // 400
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API DeleteMyFeedback EXCEPTION] FeedbackId {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi xóa đánh giá." });
            }
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFeedback()
        {
            try
            {
                var result = await _feedbackService.GetAllFeedbackAsync();
                if (result.Success)
                {
                    return Ok(result);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetAllFeedback EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy danh sách đánh giá." });
            }
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            try
            {
                // Hàm này của Admin, không liên quan đến user đang đăng nhập
                var result = await _feedbackService.DeleteFeedbackAsync(id);
                if (result.Success)
                {
                    return Ok(result);
                }
                // Service trả về false nếu không tìm thấy
                return NotFound(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API DeleteFeedback EXCEPTION] FeedbackId {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi xóa đánh giá." });
            }
        }
    }
}