using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly EcoWrapDbContext _context;

        public AdminRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Admin>> GetAllAsync()
        {
            return await _context.Admins
                .Include(a => a.User)
                .ToListAsync();
        }

        public async Task<Admin?> GetByIdAsync(int adminId)
        {
            return await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == adminId);
        }

        public async Task AddAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
        }

        public async Task UpdateAsync(Admin admin)
        {
            _context.Admins.Update(admin);
        }

        public async Task DeleteAsync(Admin admin)
        {
            _context.Admins.Remove(admin);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _context.Admins.AnyAsync(a => a.UserId == userId);
        }
    }
}