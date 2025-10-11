using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IRegistrationRepository
    {
        Task AddAsync(Registration registration);
        Task<Registration?> GetByIdAsync(int registrationId);
        Task<IEnumerable<Registration>> GetAllAsync();
        Task UpdateAsync(Registration registration);
        Task<IEnumerable<Registration>> GetByUserIdAsync(int userId);
        Task DeleteAsync(Registration registration);
        Task DeleteAsync(int registrationId);
        Task<int> SaveChangesAsync();
        Task<IEnumerable<Registration>> GetByStatusAsync(string status);
        Task<Registration?> GetPendingByUserIdAsync(int userId);
        Task<bool> ExistsAsync(int registrationId);
    }
}