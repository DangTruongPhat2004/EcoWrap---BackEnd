using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterAccountController : ControllerBase
    {
        private readonly IRegisterAccountService _registerAccountService;

        public RegisterAccountController(IRegisterAccountService registerAccountService)
        {
            _registerAccountService = registerAccountService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _registerAccountService.RegisterAccountAsync(request);

            if (!success)
            {
                return Conflict(new
                {
                    message = "Đăng ký thất bại. Username hoặc Email đã tồn tại, hoặc có lỗi xảy ra."
                });
            }

            return Ok(new
            {
                message = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.",
                email = request.Email,
                username = request.Username
            });
        }
    }
}