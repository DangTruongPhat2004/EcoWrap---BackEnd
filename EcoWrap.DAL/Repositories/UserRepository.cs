using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly EcoWrapDbContext _context;

        public UserRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Customers)
                .Include(u => u.Sellers)
                .Include(u => u.Admins)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Customers)
                .Include(u => u.Sellers)
                .Include(u => u.Admins)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByUsernameOrEmailAsync(string username, string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
        }

        public async Task<User?> GetVerifyEmailStatus(bool isVerified)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.IsVerified == isVerified);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null)
        {
            // Build the query step-by-step
            var query = _context.Users
                .Where(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase)); // Case-insensitive check

            // If an ID to exclude is provided, add another Where clause
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            // Check if any user matches the final query
            return await query.AnyAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null)
        {
            // Build the query step-by-step
            var query = _context.Users
                .Where(u => u.Email.Equals(email, System.StringComparison.OrdinalIgnoreCase)); // Case-insensitive check

            // If an ID to exclude is provided, add another Where clause
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            // Check if any user matches the final query
            return await query.AnyAsync();
        }

        public async Task<(int TotalVerified, int TotalCustomers, int TotalSellers)> GetUserMetricsAsync()
        {
            // Định nghĩa Role ID theo CSDL của bạn
            const int CUSTOMER_ROLE_ID = 2;
            const int SELLER_ROLE_ID = 3;

            // 1. Lọc các User đã xác thực (IsVerified = 1)
            // 2. Group by RoleId và đếm số lượng mỗi nhóm
            var metrics = await _context.Users
                .Where(u => u.IsVerified == true)
                .GroupBy(u => u.RoleId) // Group theo RoleId (hoặc RoleId ?? 0 nếu RoleId có thể null)
                .Select(g => new
                {
                    RoleId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 3. Xử lý kết quả
            // Lấy tổng (tất cả các nhóm đã xác thực)
            int totalVerified = metrics.Sum(m => m.Count);

            // Lấy riêng Customer (Role 2)
            int totalCustomers = metrics.FirstOrDefault(m => m.RoleId == CUSTOMER_ROLE_ID)?.Count ?? 0;

            // Lấy riêng Seller (Role 3)
            int totalSellers = metrics.FirstOrDefault(m => m.RoleId == SELLER_ROLE_ID)?.Count ?? 0;

            return (totalVerified, totalCustomers, totalSellers);
        }
    }
}