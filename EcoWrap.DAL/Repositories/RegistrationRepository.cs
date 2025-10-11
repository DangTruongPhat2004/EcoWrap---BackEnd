using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class RegistrationRepository : IRegistrationRepository
    {
        private readonly EcoWrapDbContext _context;

        public RegistrationRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Registration registration)
        {
            await _context.Registrations.AddAsync(registration);
            await _context.SaveChangesAsync(); 
        }

        public async Task<IEnumerable<Registration>> GetAllAsync()
        {
            return await _context.Registrations
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task<Registration?> GetByIdAsync(int registrationId)
        {
            return await _context.Registrations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);
        }

        public async Task<IEnumerable<Registration>> GetByUserIdAsync(int userId)
        {
            return await _context.Registrations
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task UpdateAsync(Registration registration)
        {
            _context.Registrations.Update(registration);
            await _context.SaveChangesAsync(); 
        }

        public async Task DeleteAsync(int registrationId)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration != null)
            {
                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();
            }
        }
        public async Task DeleteAsync(Registration registration)
        {
            _context.Registrations.Remove(registration);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Registration>> GetByStatusAsync(string status)
        {
            return await _context.Registrations
                .Where(r => r.ApprovalStatus == status)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task<Registration?> GetPendingByUserIdAsync(int userId)
        {
            return await _context.Registrations
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ApprovalStatus == "Pending");
        }

        public async Task<bool> ExistsAsync(int registrationId)
        {
            return await _context.Registrations.AnyAsync(r => r.RegistrationId == registrationId);
        }
    }
}