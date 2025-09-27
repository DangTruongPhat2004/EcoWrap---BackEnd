using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IRegisterAccountRepository
    {
        // User
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);

        // Email Verification
        Task AddVerificationAsync(EmailVerification verification);
        Task<EmailVerification?> GetVerificationAsync(int userId, string code);
        Task MarkVerificationAsUsedAsync(EmailVerification verification);

        Task SaveChangesAsync();
    }
}