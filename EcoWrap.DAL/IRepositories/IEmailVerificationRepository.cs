using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IEmailVerificationRepository
    {
        Task<EmailVerification?> GetByEmailAndCodeAsync(string email, string code);
        Task<IEnumerable<EmailVerification>> GetByUserIdAsync(int userId);
        Task AddAsync(EmailVerification verification);
        Task MarkAsUsedAsync(EmailVerification verification);
        Task<bool> IsEmailVerifiedAsync(int userId);
        Task<int> SaveChangesAsync();
    }
}