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
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly EcoWrapDbContext _context;

        public EmailVerificationRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<EmailVerification?> GetByEmailAndCodeAsync(string email, string code)
        {
            return await _context.EmailVerifications
                .Include(ev => ev.User)
                .FirstOrDefaultAsync(ev =>
                    ev.User.Email == email &&
                    ev.VerificationCode == code &&
                    ev.IsUsed == false &&
                    ev.Expiration > DateTime.UtcNow
                );
        }

        public async Task AddAsync(EmailVerification verification)
        {
            await _context.EmailVerifications.AddAsync(verification);
        }

        public async Task MarkAsUsedAsync(EmailVerification verification)
        {
            verification.IsUsed = true;

            // Đánh dấu User là đã xác minh
            if (verification.User != null)
            {
                verification.User.IsVerified = true;
                _context.Users.Update(verification.User);
            }

            _context.EmailVerifications.Update(verification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsEmailVerifiedAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null && user.IsVerified;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}