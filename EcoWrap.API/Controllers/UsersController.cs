using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User user)
        {
            if (id != user.UserId) return BadRequest();

            await _userService.UpdateUserAsync(user);
            return NoContent();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var claim = User.FindFirst("id");
            if (claim == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc không có claim 'id'" });
            }

            var userId = int.Parse(claim.Value);

            var success = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

            if (!success)
                return BadRequest(new { message = "Mật khẩu cũ không đúng." });

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteUserAsync(id);
            return NoContent();
        }
    }
}