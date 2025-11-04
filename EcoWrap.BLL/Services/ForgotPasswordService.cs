using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class ForgotPasswordService : IForgotPasswordService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationRepository _verificationRepository;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ForgotPasswordService(
            IUserRepository userRepository,
            IEmailVerificationRepository verificationRepository,
            IEmailService emailService,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _verificationRepository = verificationRepository;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> SendResetCodeAsync(string email)
        {
            // 1. Check email có trong hệ thống không
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

            // 2. Sinh OTP mới
            var otp = GenerateOtp();

            // 3. Lưu OTP vào DB
            var verification = new EmailVerification
            {
                UserId = user.UserId,
                VerificationCode = otp,
                Expiration = DateTime.UtcNow.AddHours(7).AddMinutes(15),
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _verificationRepository.AddAsync(verification);
            await _verificationRepository.SaveChangesAsync(); // 🔑 commit OTP vào DB

            // 4. Gửi email OTP
            await _emailService.SendEmailAsync(
                user.Email,
                "EcoWrap - Reset mật khẩu",
                $"Mã xác thực đặt lại mật khẩu của bạn là: <b>{otp}</b>. Mã này sẽ hết hạn sau 15 phút."
            );

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            // 1. Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

            // 2. Lấy OTP mới nhất từ DB
            var verifications = await _verificationRepository.GetByUserIdAsync(user.UserId);
            var verification = verifications
                .Where(v => v.VerificationCode == otp)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            // 3. Nếu OTP sai hoặc hết hạn → fail
            if (verification == null || verification.Expiration <= DateTime.UtcNow.AddHours(7))
                return false;

            // 4. Hash và cập nhật mật khẩu
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync(); // 🔑 commit update user

            return true;
        }

        private string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            return code.ToString("D6");
        }
    }
}