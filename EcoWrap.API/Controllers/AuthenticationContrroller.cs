using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var token = await _authenticationService.LoginAsync(request);

                if (token == null)
                    return Unauthorized(new { Message = "Tên đăng nhập hoặc mật khẩu không đúng." });

                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Email chưa xác thực
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception)
            {
                // Lỗi khác
                return StatusCode(500, new { Message = "Đã xảy ra lỗi trong quá trình đăng nhập." });
            }
        }
    }
}