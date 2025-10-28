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
    }
}