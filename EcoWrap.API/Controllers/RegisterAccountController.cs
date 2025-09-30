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

        /// <summary>
        /// Đăng ký tài khoản mới và gửi email xác nhận
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _registerAccountService.RegisterAccountAsync(request);

            if (!success)
            {
                return Conflict(new
                {
                    message = "Đăng ký thất bại. Email đã tồn tại hoặc có lỗi xảy ra."
                });
            }

            return CreatedAtAction(
                nameof(Register),
                new { email = request.Email },
                new
                {
                    message = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.",
                    email = request.Email,
                    username = request.Username
                });
        }
    }
}