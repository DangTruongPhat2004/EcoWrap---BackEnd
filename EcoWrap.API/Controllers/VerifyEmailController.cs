using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyEmailController : ControllerBase
    {
        private readonly IEmailVerificationService _verificationService;

        public VerifyEmailController(IEmailVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.VerifyEmailAsync(request);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Mã xác nhận không hợp lệ hoặc đã hết hạn."
                });
            }

            return Ok(new
            {
                message = "Xác nhận email thành công.",
                email = request.Email,
                isVerified = true
            });
        }

        [HttpGet("status/{userId:int}")]
        public async Task<IActionResult> CheckStatus([FromRoute] int userId)
        {
            var isVerified = await _verificationService.IsEmailVerifiedAsync(userId);

            return Ok(new
            {
                userId,
                isVerified,
                message = isVerified
                    ? "Email đã được xác minh."
                    : "Email chưa được xác minh."
            });
        }
    }
}