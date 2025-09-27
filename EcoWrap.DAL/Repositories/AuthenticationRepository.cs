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
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly EcoWrapDbContext _context;

        public AuthenticationRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}