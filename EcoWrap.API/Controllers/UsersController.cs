using EcoWrap.BLL.IServices;
using EcoWrap.Models.Request; // Cần thiết cho UpdateUserRequest, ChangePasswordRequest
using EcoWrap.Models.Response; // Cần thiết cho UserResponse
using EcoWrap.Models.Responses; // Cần thiết cho ApiResponse (nếu dùng để bao bọc lỗi)
using Microsoft.AspNetCore.Authorization; // Cần thiết cho [Authorize]
using Microsoft.AspNetCore.Http; // Cần thiết cho StatusCodes
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims; // Cần thiết cho ClaimTypes
using System.Threading.Tasks;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu đăng nhập cho tất cả các action trong controller này
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // --- HELPER: Get User ID from Token ---
        private IActionResult GetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out userId))
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID không hợp lệ trong token." });
            }
            return null; // Thành công
        }

        // --- GET ALL USERS (ADMIN ONLY) ---
        [HttpGet]
        [Authorize(Roles = "Admin")] // Chỉ Admin được xem tất cả user
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                // Bọc kết quả trong ApiResponse nếu muốn thống nhất
                return Ok(new ApiResponse<IEnumerable<UserResponse>> { Success = true, Data = users });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetAllUsers EXCEPTION]: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy danh sách người dùng." });
            }
        }

        // --- GET USER BY ID ---
        [HttpGet("{id}")]
        [Authorize] // Bất kỳ ai đăng nhập cũng có thể gọi, nhưng logic service cần kiểm tra quyền
        public async Task<IActionResult> GetById(int id)
        {
            // Kiểm tra xem người gọi có phải là admin hoặc chính user đó không
            var unauthorizedResult = GetCurrentUserId(out int currentUserId);
            if (unauthorizedResult != null) return unauthorizedResult; // Should not happen if [Authorize] works

            bool isAdmin = User.IsInRole("Admin");

            //if (!isAdmin && currentUserId != id)
            //{
            //    // Nếu không phải Admin và không phải đang xem profile của chính mình -> Forbidden
            //    return Forbid(); // Trả về 403 Forbidden
            //}

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Không tìm thấy người dùng với ID: {id}" });
                }
                return Ok(new ApiResponse<UserResponse> { Success = true, Data = user });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetUserById EXCEPTION] Id {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy thông tin người dùng." });
            }
        }

        // --- GET MY PROFILE ---
        [HttpGet("profile")]
        [Authorize] // Chỉ cần đăng nhập
        public async Task<IActionResult> GetMyProfile()
        {
            var unauthorizedResult = GetCurrentUserId(out int userId);
            if (unauthorizedResult != null) return unauthorizedResult;

            try
            {
                // Dùng lại hàm GetUserByIdAsync vì nó trả về đủ thông tin
                var userProfile = await _userService.GetUserByIdAsync(userId);

                if (userProfile == null)
                {
                    // Trường hợp hiếm: token hợp lệ nhưng user đã bị xóa
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy thông tin người dùng." });
                }
                return Ok(new ApiResponse<UserResponse> { Success = true, Data = userProfile });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API GetMyProfile EXCEPTION] UserId {userId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi lấy thông tin hồ sơ." });
            }
        }


        // --- UPDATE USER PROFILE ---
        [HttpPut("{id}")]
        [Authorize] // Yêu cầu đăng nhập
        [Consumes("multipart/form-data")] // Cần thiết vì có IFormFile (ProfilePicture)
        public async Task<IActionResult> Update(int id, [FromForm] UpdateUserRequest request) // Sử dụng [FromForm]
        {
            // Kiểm tra quyền: Hoặc là Admin, hoặc là user đang cập nhật profile của chính mình
            var unauthorizedResult = GetCurrentUserId(out int currentUserId);
            if (unauthorizedResult != null) return unauthorizedResult;

            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && currentUserId != id)
            {
                return Forbid(); // Không được phép cập nhật profile người khác nếu không phải Admin
            }

            try
            {
                // Gọi service với id từ route và request từ form
                var updatedUser = await _userService.UpdateUserAsync(id, request);
                // Service sẽ throw exception nếu user không tồn tại hoặc có lỗi (vd: username/email trùng)
                return Ok(new ApiResponse<UserResponse> { Success = true, Data = updatedUser, Message = "Cập nhật hồ sơ thành công." });
            }
            catch (ArgumentException ex) // Bắt lỗi User không tìm thấy từ Service
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex) // Bắt lỗi trùng Username/Email hoặc lỗi upload ảnh từ Service
            {
                // Trả về 409 Conflict nếu là lỗi trùng lặp
                if (ex.Message.Contains("đã được sử dụng"))
                {
                    return Conflict(new ApiResponse<object> { Success = false, Message = ex.Message });
                }
                // Trả về 400 Bad Request cho các lỗi khác (vd: upload ảnh)
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex) // Lỗi hệ thống khác
            {
                Console.WriteLine($"[API UpdateUser EXCEPTION] Id {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi cập nhật hồ sơ." });
            }
        }

        // --- CHANGE PASSWORD ---
        [HttpPost("change-password")]
        [Authorize] // Chỉ cần đăng nhập là được đổi pass của chính mình
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var unauthorizedResult = GetCurrentUserId(out int userId);
            if (unauthorizedResult != null) return unauthorizedResult;

            // Validate request
            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Mật khẩu cũ và mới không được để trống." });
            }
            if (request.NewPassword.Length < 6) // Ví dụ: kiểm tra độ dài tối thiểu
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
            }

            try
            {
                var success = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

                if (!success)
                {
                    // Lý do thất bại có thể là user không tồn tại (ít khả năng) hoặc sai mật khẩu cũ
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Mật khẩu cũ không đúng hoặc người dùng không tồn tại." });
                }

                return Ok(new ApiResponse<object> { Success = true, Message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ChangePassword EXCEPTION] UserId {userId}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi đổi mật khẩu." });
            }
        }

        // --- DELETE USER (ADMIN ONLY) ---
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Chỉ Admin được xóa
        public async Task<IActionResult> Delete(int id)
        {
            // Tùy chọn: Ngăn Admin tự xóa chính mình?
            var unauthorizedResult = GetCurrentUserId(out int currentUserId);
            if (unauthorizedResult == null && currentUserId == id)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Không thể tự xóa tài khoản của chính mình." });
            }


            try
            {
                await _userService.DeleteUserAsync(id);
                // Trả về 204 No Content là chuẩn cho Delete thành công
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("User not found")) // Bắt lỗi UserNotFound từ Service
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = $"Không tìm thấy người dùng với ID: {id}" });
            }
            // Bắt lỗi nếu không xóa được do ràng buộc khóa ngoại (ví dụ: User đã có Order, Product...)
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"[API DeleteUser DB EXCEPTION] Id {id}: {dbEx.ToString()}");
                // Trả về 409 Conflict
                return Conflict(new ApiResponse<object> { Success = false, Message = "Không thể xóa người dùng này vì có dữ liệu liên quan (đơn hàng, sản phẩm...). Hãy xem xét việc vô hiệu hóa tài khoản thay vì xóa." });
            }
            catch (Exception ex) // Lỗi hệ thống khác
            {
                Console.WriteLine($"[API DeleteUser EXCEPTION] Id {id}: {ex.ToString()}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "Lỗi hệ thống khi xóa người dùng." });
            }
        }
    }
}