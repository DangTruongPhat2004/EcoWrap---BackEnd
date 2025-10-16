using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Enum;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.BLL.Services
{
    public class SellerRegistrationService : ISellerRegistrationService
    {
        private readonly IRegistrationRepository _registrationRepo;
        private readonly ISellerRepository _sellerRepo;
        private readonly IUserRepository _userRepo;
        private readonly CloudinaryService _cloudinary;
        private readonly IEmailService _emailService;
        private readonly EcoWrapDbContext _dbContext;

        public SellerRegistrationService(
            IRegistrationRepository registrationRepo,
            ISellerRepository sellerRepo,
            IUserRepository userRepo,
            CloudinaryService cloudinary,
            IEmailService emailService,
            EcoWrapDbContext dbContext)  // inject DbContext
        {
            _registrationRepo = registrationRepo ?? throw new ArgumentNullException(nameof(registrationRepo));
            _sellerRepo = sellerRepo ?? throw new ArgumentNullException(nameof(sellerRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // ------------------------ [SUBMIT REGISTRATION] ------------------------
        public async Task<bool> SubmitRegistrationAsync(int userId, RegisterSellerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _userRepo.GetByIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

            // Kiểm tra nếu user đã có đơn pending
            var existing = await _registrationRepo.GetPendingByUserIdAsync(userId);
            if (existing != null)
                throw new InvalidOperationException("Bạn đã có một đơn đăng ký đang được xử lý.");

            string? personalIdUrl = null;
            string? licenseFileUrl = null;

            try
            {
                // Upload ảnh CMND/CCCD
                if (request.PersonalIdImage != null && request.PersonalIdImage.Length > 0)
                {
                    personalIdUrl = await UploadSmartAsync(request.PersonalIdImage, "ecowrap/ids");
                    if (string.IsNullOrEmpty(personalIdUrl))
                        throw new InvalidOperationException("Không thể tải lên ảnh CMND/CCCD.");
                }

                // Upload giấy phép kinh doanh
                if (request.BusinessLicenseFile != null && request.BusinessLicenseFile.Length > 0)
                {
                    licenseFileUrl = await UploadSmartAsync(request.BusinessLicenseFile, "ecowrap/licenses");
                    if (string.IsNullOrEmpty(licenseFileUrl))
                        throw new InvalidOperationException("Không thể tải lên giấy phép kinh doanh.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi tải tệp lên Cloudinary.", ex);
            }

            // Lưu thông tin đăng ký
            var registration = new Registration
            {
                UserId = userId,
                CompanyName = request.CompanyName?.Trim(),
                CompanyAddress = request.CompanyAddress?.Trim(),
                CompanyPhone = request.CompanyPhone?.Trim(),
                LicenseId = request.LicenseId?.Trim(),
                PersonalIdImage = personalIdUrl,
                DocumentPath = licenseFileUrl,
                RequestedDate = DateTime.UtcNow.AddHours(7),
                ApprovalStatus = RegistrationStatus.Pending.ToString()
            };

            await _registrationRepo.AddAsync(registration);
            await _registrationRepo.SaveChangesAsync();

            return true;
        }

        // ------------------------ [SMART UPLOAD HELPER] ------------------------
        private async Task<string> UploadSmartAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var extension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("File không có phần mở rộng hợp lệ.");

            // Các phần mở rộng hình ảnh phổ biến
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg" };

            if (imageExtensions.Contains(extension))
            {
                // Upload ảnh
                return await _cloudinary.UploadImageAsync(file, folder);
            }
            else
            {
                // Upload tài liệu, pdf, zip,...
                return await _cloudinary.UploadFileAsync(file, folder);
            }
        }

        // ------------------------ [GETTERS] ------------------------
        public async Task<IEnumerable<Registration>> GetAllRegistrationsAsync()
            => await _registrationRepo.GetAllAsync();

        public async Task<IEnumerable<Registration>> GetRegistrationsByUserIdAsync(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId không hợp lệ.", nameof(userId));

            return await _registrationRepo.GetByUserIdAsync(userId);
        }

        public async Task<Registration?> GetRegistrationByIdAsync(int registrationId)
        {
            if (registrationId <= 0)
                throw new ArgumentException("Registration ID không hợp lệ.", nameof(registrationId));

            return await _registrationRepo.GetByIdAsync(registrationId);
        }

        public async Task<IEnumerable<Registration>> GetRegistrationsByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Trạng thái không hợp lệ.", nameof(status));

            return await _registrationRepo.GetByStatusAsync(status);
        }

        // ------------------------ [APPROVE] ------------------------
        public async Task<bool> ApproveRegistrationAsync(int registrationId)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn đăng ký.");

            if (registration.ApprovalStatus != RegistrationStatus.Pending.ToString())
                throw new InvalidOperationException("Chỉ có thể duyệt các đơn đang chờ xử lý.");

            var existingSeller = await _sellerRepo.GetByUserIdAsync(registration.UserId);
            if (existingSeller != null)
                throw new InvalidOperationException("Người dùng này đã là người bán.");

            var user = await _userRepo.GetByIdAsync(registration.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1️ Cập nhật trạng thái đơn
                registration.ApprovalStatus = RegistrationStatus.Approved.ToString();
                await _registrationRepo.UpdateAsync(registration);

                // 2️ Cập nhật RoleId của user = 3 (Seller)
                user.RoleId = 3;
                await _userRepo.UpdateAsync(user);

                await _registrationRepo.SaveChangesAsync();
                await _userRepo.SaveChangesAsync();

                // 3️ Tạo record Seller
                var seller = new Seller
                {
                    UserId = registration.UserId,
                    CompanyName = registration.CompanyName,
                    Address = registration.CompanyAddress
                };
                await _sellerRepo.AddAsync(seller);
                await _sellerRepo.SaveChangesAsync();

                await transaction.CommitAsync();

                // 4️ Gửi email thông báo
                var subject = "EcoWrap - Đơn đăng ký người bán đã được duyệt";
                var body = $@"
                <p>Xin chào <b>{user.FullName}</b>,</p>
                <p>Chúc mừng! Đơn đăng ký người bán của bạn trên <b>EcoWrap</b> đã được <b>duyệt</b>.</p>
                <p>Bây giờ bạn có thể đăng sản phẩm và bắt đầu kinh doanh ngay hôm nay!</p>
                <p>Trân trọng,<br>Đội ngũ EcoWrap</p>";

                await _emailService.SendEmailAsync(user.Email, subject, body);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UserRejectTheirRegistrationAsync(int registrationId, int userId)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn đăng ký.");

            //Kiểm tra: chỉ chủ đơn mới được phép hủy
            if (registration.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn đăng ký của người khác.");

            if (registration.ApprovalStatus == RegistrationStatus.Approved.ToString())
                throw new InvalidOperationException("Không thể hủy đơn đã được duyệt.");

            var user = await _userRepo.GetByIdAsync(registration.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

            registration.ApprovalStatus = RegistrationStatus.Canceled.ToString();
            await _registrationRepo.UpdateAsync(registration);
            await _registrationRepo.SaveChangesAsync();

            var subject = "EcoWrap - Đơn đăng ký người bán bị hủy bởi người dùng";
            var body = $@"
        <p>Xin chào <b>{user.FullName}</b>,</p>
        <p>Bạn vừa hủy đơn đăng ký làm người bán của bạn trên <b>EcoWrap</b>.</p>
        <p>Trân trọng,<br>Đội ngũ EcoWrap</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return true;
        }

        public async Task<bool> AdminRejectUserRegistrationAsync(int registrationId, string? reason)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn đăng ký.");

            if (registration.ApprovalStatus != RegistrationStatus.Pending.ToString())
                throw new InvalidOperationException("Chỉ có thể từ chối các đơn đang chờ xử lý.");

            var user = await _userRepo.GetByIdAsync(registration.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

            registration.ApprovalStatus = RegistrationStatus.Canceled.ToString();
            await _registrationRepo.UpdateAsync(registration);
            await _registrationRepo.SaveChangesAsync();

            // Gửi email thông báo từ chối (có thể có lý do)
            var subject = "EcoWrap - Đơn đăng ký người bán bị từ chối";

            var body = $@"
        <p>Xin chào <b>{user.FullName}</b>,</p>
        <p>Rất tiếc, đơn đăng ký người bán của bạn trên <b>EcoWrap</b> đã bị <b>từ chối</b>.</p>";

            if (!string.IsNullOrWhiteSpace(reason))
                body += $"<p><b>Lý do:</b> {System.Net.WebUtility.HtmlEncode(reason)}</p>";

            body += @"<p>Vui lòng kiểm tra lại thông tin đã gửi hoặc liên hệ đội ngũ hỗ trợ để biết thêm chi tiết.</p>
              <p>Trân trọng,<br>Đội ngũ EcoWrap</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return true;
        }
    }
}