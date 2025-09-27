using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.DAL.Repositories;

namespace EcoWrap.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task CreateUserAsync(User user)
        {
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync(); 
        }

        public async Task UpdateUserAsync(User user)
        {
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();  
        }

        public async Task DeleteUserAsync(int id)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
                throw new Exception("User not found");

            await _userRepository.DeleteAsync(id);
            await _userRepository.SaveChangesAsync();
        }
    }
}