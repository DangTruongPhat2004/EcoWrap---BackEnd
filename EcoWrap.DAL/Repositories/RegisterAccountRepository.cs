using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
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
                    v.IsUsed == false &&
                    v.Expiration > DateTime.Now);
        }

        public async Task MarkVerificationAsUsedAsync(EmailVerification verification)
        {
            verification.IsUsed = true;
            _context.EmailVerifications.Update(verification);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}