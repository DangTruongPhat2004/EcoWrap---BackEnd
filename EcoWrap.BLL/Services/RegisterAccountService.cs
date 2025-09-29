using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services
{
    public class RegisterAccountService : IRegisterAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmailVerificationRepository _verificationRepository;
        private readonly IEmailService _emailService;
        private readonly EcoWrapDbContext _dbContext; // để dùng transaction

        public RegisterAccountService(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IEmailVerificationRepository verificationRepository,
            IEmailService emailService,
            EcoWrapDbContext dbContext)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _verificationRepository = verificationRepository;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public async Task<bool> RegisterAccountAsync(RegisterAccountRequest request)
        {
            // 1. Kiểm tra Email trùng
            var existedUserByEmail = await _userRepository.GetByEmailAsync(request.Email);

            if (existedUserByEmail != null)
            {
                if (existedUserByEmail.IsVerified)
                {
                    // Nếu email đã verify rồi => không cho đăng ký lại
                    return false;
                }
                else
                {
                    // Nếu email tồn tại nhưng chưa verify => xóa account cũ để đăng ký lại
                    _dbContext.Users.Remove(existedUserByEmail);

                    // Xóa luôn bản ghi Customer liên quan (nếu có)
                    var oldCustomer = await _customerRepository.GetByUserIdAsync(existedUserByEmail.UserId);
                    if (oldCustomer != null)
                    {
                        _dbContext.Customers.Remove(oldCustomer);
                    }

                    // Xóa các bản ghi verification cũ
                    var oldVerifications = await _verificationRepository.GetByUserIdAsync(existedUserByEmail.UserId);
                    if (oldVerifications.Any())
                    {
                        _dbContext.EmailVerifications.RemoveRange(oldVerifications);
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }

            // 2. Hash password bằng SHA256
            string hashedPassword = HashPasswordSHA256(request.Password);

            // 3. Tạo User entity (chưa verified)
            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = hashedPassword,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                RegistrationDate = DateTime.UtcNow,
                IsVerified = false
            };

            // 4. Transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 5. Lưu User
                await _userRepository.AddAsync(newUser);
                await _dbContext.SaveChangesAsync(); // để lấy UserId

                // 6. Lưu Customer
                var newCustomer = new Customer { UserId = newUser.UserId };
                await _customerRepository.AddAsync(newCustomer);
                await _dbContext.SaveChangesAsync();

                // 7. Sinh mã xác nhận email (6 số)
                var confirmCode = GenerateVerificationCode();

                // 8. Lưu vào EmailVerifications
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

                // 9. Commit transaction
                await transaction.CommitAsync();

                // 10. Gửi email xác nhận
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

        private string HashPasswordSHA256(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = BitConverter.ToInt32(bytes, 0);
            code = Math.Abs(code % 1000000); // 6 số
            return code.ToString("D6");
        }
    }
}