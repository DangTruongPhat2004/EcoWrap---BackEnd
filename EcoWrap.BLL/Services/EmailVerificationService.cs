using System.Security.Cryptography;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.DAL.Repositories;
using EcoWrap.Models.Request;
using Microsoft.EntityFrameworkCore.Storage;

namespace EcoWrap.BLL.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IEmailVerificationRepository _verificationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly EcoWrapDbContext _dbContext;

        public EmailVerificationService(
            IEmailVerificationRepository verificationRepo,
            EcoWrapDbContext dbContext)
        {
            _verificationRepo = verificationRepo;
            _dbContext = dbContext;
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var verification = await _verificationRepo.GetByEmailAndCodeAsync(
                    request.Email,
                    request.VerificationCode
                );

                if (verification == null)
                    return false; // Không tồn tại hoặc đã hết hạn

                // Đánh dấu đã sử dụng + cập nhật User.IsVerified
                await _verificationRepo.MarkAsUsedAsync(verification);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsEmailVerifiedAsync(int userId)
        {
            return await _verificationRepo.IsEmailVerifiedAsync(userId);
        }
    }
}