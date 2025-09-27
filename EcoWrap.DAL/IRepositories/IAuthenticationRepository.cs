using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IAuthenticationRepository
    {
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<User?> GetByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task SaveChangesAsync();
    }
}