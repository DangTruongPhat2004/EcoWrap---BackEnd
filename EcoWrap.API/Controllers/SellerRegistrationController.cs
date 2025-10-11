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
    public class SellerRegistrationController : ControllerBase
    {
        private readonly ISellerRegistrationService _registrationService;

        public SellerRegistrationController(ISellerRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        // ------------------------- [Gửi đơn đăng ký người bán] -------------------------
        [HttpPost("register")]
        [Authorize] // Chỉ user đã đăng nhập mới được gửi đơn
        public async Task<IActionResult> RegisterSeller([FromForm] RegisterSellerRequest request)
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Dữ liệu người dùng không hợp lệ." });

                var result = await _registrationService.SubmitRegistrationAsync(userId, request);
                if (result)
                    return Ok(new { message = "Đơn đăng ký đã được gửi thành công." });

                return BadRequest(new { message = "Gửi đơn đăng ký thất bại." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        // ------------------------- [Lấy tất cả đơn đăng ký (Admin)] -------------------------
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRegistrations()
        {
            var registrations = await _registrationService.GetAllRegistrationsAsync();
            return Ok(registrations);
        }

        // ------------------------- [Lấy danh sách đơn của 1 người dùng] -------------------------
        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetRegistrationsByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "User ID không hợp lệ." });

            var registrations = await _registrationService.GetRegistrationsByUserIdAsync(userId);
            return Ok(registrations);
        }

        // ------------------------- [Lấy đơn đăng ký theo ID] -------------------------
        [HttpGet("{registrationId:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int registrationId)
        {
            if (registrationId <= 0)
                return BadRequest(new { message = "Registration ID không hợp lệ." });

            var registration = await _registrationService.GetRegistrationByIdAsync(registrationId);
            if (registration == null)
                return NotFound(new { message = "Không tìm thấy đơn đăng ký." });

            return Ok(registration);
        }

        // ------------------------- [Lọc đơn theo trạng thái (Pending, Approved, Canceled)] -------------------------
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Trạng thái không hợp lệ." });

            var registrations = await _registrationService.GetRegistrationsByStatusAsync(status);
            return Ok(registrations);
        }

        // ------------------------- [Admin phê duyệt đơn đăng ký] -------------------------
        [HttpPut("approve/{registrationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int registrationId)
        {
            try
            {
                var result = await _registrationService.ApproveRegistrationAsync(registrationId);
                if (result)
                    return Ok(new { message = "Đơn đăng ký đã được phê duyệt." });

                return BadRequest(new { message = "Phê duyệt thất bại." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ------------------------- [Admin từ chối đơn đăng ký] -------------------------
        [HttpPut("reject/{registrationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReject(int registrationId, [FromBody] AdminRejectRequest request)
        {
            try
            {
                var result = await _registrationService.AdminRejectUserRegistrationAsync(registrationId, request?.Reason);
                if (result)
                    return Ok(new { message = "Bạn đã hủy đơn đăng ký của User này thành công." });

                return BadRequest(new { message = "Từ chối thất bại." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ------------------------- [User tự hủy đơn của mình] -------------------------
        [HttpPut("user/cancel/{registrationId:int}")]
        [Authorize]
        public async Task<IActionResult> UserRejectRegistration(int registrationId)
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            try
            {
                var result = await _registrationService.UserRejectTheirRegistrationAsync(registrationId, userId);
                if (result)
                    return Ok(new { message = "Đơn đăng ký đã được hủy thành công." });

                return BadRequest(new { message = "Hủy đơn đăng ký thất bại." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}