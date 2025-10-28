using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();
        Task<UserResponse?> GetUserByIdAsync(int id);
        Task<UserResponse> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<User?> GetUserEntityByIdAsync(int id);
        Task DeleteUserAsync(int id);
    }
}