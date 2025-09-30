using System;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class RegisterAccountRepository : IRegisterAccountRepository
    {
        private readonly EcoWrapDbContext _context;

        public RegisterAccountRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        // User
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        // Email Verification
        public async Task AddVerificationAsync(EmailVerification verification)
        {
            await _context.EmailVerifications.AddAsync(verification);
        }

        public async Task<EmailVerification?> GetVerificationAsync(int userId, string code)
        {
            return await _context.EmailVerifications
                .FirstOrDefaultAsync(v =>
                    v.UserId == userId &&
                    v.VerificationCode == code &&
                    !v.IsUsed &&
                    v.Expiration > DateTime.UtcNow);
        }

        public Task MarkVerificationAsUsedAsync(EmailVerification verification)
        {
            verification.IsUsed = true;
            _context.EmailVerifications.Update(verification);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}