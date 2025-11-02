using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameOrEmailAsync(string username, string email);
        Task<User?> GetVerifyEmailStatus (bool isVerified);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<int> SaveChangesAsync(); // thêm để quản lý transaction
        Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null);
        Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null);
        Task<(int TotalVerified, int TotalCustomers, int TotalSellers)> GetUserMetricsAsync();
    }
}