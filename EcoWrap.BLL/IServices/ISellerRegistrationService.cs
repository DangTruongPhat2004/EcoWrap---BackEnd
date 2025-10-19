using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.IServices
{
    public interface ISellerRegistrationService
    {
        // ----- Đăng ký người bán -----
        Task<bool> SubmitRegistrationAsync(int userId, RegisterSellerRequest request);

        // ----- Lấy danh sách / chi tiết -----
        Task<IEnumerable<Registration>> GetAllRegistrationsAsync();
        Task<IEnumerable<Registration>> GetRegistrationsByUserIdAsync(int userId);
        Task<Registration?> GetRegistrationByIdAsync(int registrationId);
        Task<IEnumerable<Registration>> GetRegistrationsByStatusAsync(string status);

        // ----- Duyệt / Từ chối -----
        Task<bool> ApproveRegistrationAsync(int registrationId);
        Task<bool> AdminRejectUserRegistrationAsync(int registrationId, string? reason);
        Task<bool> UserRejectTheirRegistrationAsync(int registrationId, int userId);
    }
}