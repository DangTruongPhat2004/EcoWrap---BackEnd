using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly IForgotPasswordService _forgotPasswordService;

        public ForgotPasswordController(IForgotPasswordService forgotPasswordService)
        {
            _forgotPasswordService = forgotPasswordService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestReset([FromBody] ForgotPasswordRequest request)
        {
            var success = await _forgotPasswordService.SendResetCodeAsync(request.Email);
            if (!success) return NotFound(new { message = "Email không tồn tại." });

            return Ok(new { message = "Mã OTP đã gửi vào email." });
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReset([FromBody] ResetPasswordRequest request)
        {
            var success = await _forgotPasswordService.ResetPasswordAsync(request.Email, request.Otp, request.NewPassword);
            if (!success) return BadRequest(new { message = "OTP không hợp lệ hoặc đã hết hạn." });

            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }
    }
}