using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly CloudinaryService _cloudinaryService;

        public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, CloudinaryService cloudinaryService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync() 
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(user => new UserResponse
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                Username = user.Username,
                Password = user.PasswordHash,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,       
                RegistrationDate = user.RegistrationDate, 
                IsVerified = user.IsVerified
            });
        }

        // --- GetUserByIdAsync ---
        public async Task<UserResponse?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            return new UserResponse
            {
                UserId = user.UserId,
                RoleId = user.RoleId, 
                Username = user.Username,
                Password = user.PasswordHash,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,     
                RegistrationDate = user.RegistrationDate, 
                IsVerified = user.IsVerified
            };
        }

        public async Task<UserResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Yêu cầu cập nhật không được rỗng.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"Không tìm thấy người dùng với ID: {userId}.", nameof(userId));
            }

            bool changed = false;

            // --- Check Username ---
            if (!string.IsNullOrWhiteSpace(request.Username) && !user.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase))
            {
                bool usernameExists = await _userRepository.ExistsByUsernameAsync(request.Username, userId);
                if (usernameExists)
                {
                    throw new InvalidOperationException($"Username '{request.Username}' đã được sử dụng bởi tài khoản khác.");
                }
                user.Username = request.Username;
                changed = true;
                Console.WriteLine($"[UserService] Updating Username for UserId {userId}.");
            }

            // --- Check Email ---
            if (!string.IsNullOrWhiteSpace(request.Email) && !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                bool emailExists = await _userRepository.ExistsByEmailAsync(request.Email, userId);
                if (emailExists)
                {
                    throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng bởi tài khoản khác.");
                }
                user.Email = request.Email;
                changed = true;
                Console.WriteLine($"[UserService] Updating Email for UserId {userId}.");
            }

            // --- Update FullName ---
            if (request.FullName != user.FullName)
            {
                user.FullName = request.FullName;
                changed = true;
                Console.WriteLine($"[UserService] Updating FullName for UserId {userId}.");
            }

            // --- Update PhoneNumber ---
            if (request.PhoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = request.PhoneNumber;
                changed = true;
                Console.WriteLine($"[UserService] Updating PhoneNumber for UserId {userId}.");
            }

            // --- Handle Profile Picture Upload ---
            if (request.ProfilePicture != null && request.ProfilePicture.Length > 0)
            {
                Console.WriteLine($"[UserService] Attempting to upload new profile picture for UserId {userId}.");
                try
                {
                    // Optional: Delete old logic
                    var imageUrl = await _cloudinaryService.UploadImageAsync(request.ProfilePicture, $"EcoWrap/ProfilePictures/{userId}");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        user.ProfilePicture = imageUrl;
                        changed = true;
                        Console.WriteLine($"[UserService] Profile picture updated for UserId {userId}: {imageUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"[Cloudinary Warn] UploadImageAsync returned empty/null URL for UserId {userId}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cloudinary Error] Failed to upload profile picture for UserId {userId}: {ex.ToString()}");
                    throw new InvalidOperationException("Lỗi khi tải ảnh đại diện lên.", ex);
                }
            }

            // --- Save Changes ---
            if (changed)
            {
                try
                {
                    await _userRepository.SaveChangesAsync();
                    Console.WriteLine($"[UserService] User profile update saved successfully for UserId {userId}.");
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"[DB Error] Failed to save user profile update for UserId {userId}: {dbEx.ToString()}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"[UserService] No changes detected for UserId {userId} profile update.");
            }

            // --- Return Updated User (Corrected Mapping) ---
            return new UserResponse
            {
                UserId = user.UserId,
                RoleId = user.RoleId, // Handle potential null RoleId
                Username = user.Username,
                // NO PasswordHash here
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,     // Map ProfilePicture
                RegistrationDate = user.RegistrationDate, // Map RegistrationDate
                IsVerified = user.IsVerified
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Kiểm tra mật khẩu cũ
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                return false;

            // Hash mật khẩu mới và cập nhật
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<User?> GetUserEntityByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
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