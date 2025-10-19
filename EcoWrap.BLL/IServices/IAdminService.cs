using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.BLL.IServices
{
    public interface IAdminService
    {
        Task<IEnumerable<Admin>> GetAllAdminsAsync();
        Task<Admin?> GetAdminByIdAsync(int adminId);
        Task<string> CreateAdminAsync(int userId);
        Task<string> UpdateAdminAsync(int adminId, int userId);
        Task<string> DeleteAdminAsync(int adminId);
    }
}