using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services
{
    public class RegisterAccountService : IRegisterAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmailVerificationRepository _verificationRepository;
        private readonly IEmailService _emailService;
        private readonly EcoWrapDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegisterAccountService(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IEmailVerificationRepository verificationRepository,
            IEmailService emailService,
            EcoWrapDbContext dbContext,
            IPasswordHasher<User> passwordHasher) // inject vào constructor
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _verificationRepository = verificationRepository;
            _emailService = emailService;
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> RegisterAccountAsync(RegisterAccountRequest request)
        {
            // 1. Kiểm tra Email trùng
            var existedUserByEmail = await _userRepository.GetByEmailAsync(request.Email);

            if (existedUserByEmail != null)
            {
                if (existedUserByEmail.IsVerified)
                    return false;

                // Xóa user chưa xác thực
                _dbContext.Users.Remove(existedUserByEmail);

                var oldCustomer = await _customerRepository.GetByUserIdAsync(existedUserByEmail.UserId);
                if (oldCustomer != null)
                    _dbContext.Customers.Remove(oldCustomer);

                var oldVerifications = await _verificationRepository.GetByUserIdAsync(existedUserByEmail.UserId);
                if (oldVerifications.Any())
                    _dbContext.EmailVerifications.RemoveRange(oldVerifications);

                await _dbContext.SaveChangesAsync();
            }

            // 2. Tạo User mới với RoleId = 2 (Customer)
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                RegistrationDate = DateTime.UtcNow,
                IsVerified = false,
                RoleId = 2 // Customer
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 3. Lưu User
                await _userRepository.AddAsync(newUser);
                await _dbContext.SaveChangesAsync();

                // 4. Lưu Customer
                var newCustomer = new Customer { UserId = newUser.UserId };
                await _customerRepository.AddAsync(newCustomer);

                // 5. Sinh mã xác nhận
                var confirmCode = GenerateVerificationCode();

                // 6. Lưu EmailVerification
                var verification = new EmailVerification
                {
                    UserId = newUser.UserId,
                    VerificationCode = confirmCode,
                    Expiration = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _verificationRepository.AddAsync(verification);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Gửi email xác nhận
                await _emailService.SendEmailAsync(
                    newUser.Email,
                    "EcoWrap - Email Verification",
                    $@"
                Xin chào {newUser.FullName ?? newUser.Username},<br/>
                Mã xác nhận của bạn là: <b>{confirmCode}</b><br/>
                Mã này sẽ hết hạn sau 15 phút."
                );

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = BitConverter.ToInt32(bytes, 0);
            code = Math.Abs(code % 1000000);
            return code.ToString("D6");
        }
    }
}