using EcoWrap.DAL.Entities;
using EcoWrap.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();
        Task<UserResponse?> GetUserByIdAsync(int id);
        Task UpdateUserAsync(User user);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task DeleteUserAsync(int id);
    }
}