using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;

namespace EcoWrap.BLL.Services
{
    public class SellerService : ISellerService
    {
        private readonly ISellerRepository _sellerRepo;
        private readonly IRegistrationRepository _registrationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly CloudinaryService _cloudinary;

        public SellerService(
            ISellerRepository sellerRepo,
            IRegistrationRepository registrationRepo,
            IUserRepository userRepo,
            IEmailService emailService,
            CloudinaryService cloudinary)
        {
            _sellerRepo = sellerRepo ?? throw new ArgumentNullException(nameof(sellerRepo));
            _registrationRepo = registrationRepo ?? throw new ArgumentNullException(nameof(registrationRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
        }

        //GET: Lấy tất cả Seller
        public async Task<IEnumerable<SellerResponse>> GetAllAsync()
        {
            var sellers = await _sellerRepo.GetAllAsync();
            return sellers.Select(s => new SellerResponse
            {
                SellerId = s.SellerId,
                UserId = s.UserId,
                CompanyName = s.CompanyName,
                Address = s.Address,
                BankAccountNumber = s.BankAccountNumber,
                BankName = s.BankName,
                AccountHolderName = s.AccountHolderName,
                BankBranch = s.BankBranch,
                QrcodeImage = s.QrcodeImage
            });
        }

        //GET: Lấy Seller theo SellerId
        public async Task<SellerResponse?> GetBySellerIdAsync(int id)
        {
            var seller = await _sellerRepo.GetBySellerIdAsync(id);
            if (seller == null) return null;

            return new SellerResponse
            {
                SellerId = seller.SellerId,
                UserId = seller.UserId,
                CompanyName = seller.CompanyName,
                Address = seller.Address,
                BankAccountNumber = seller.BankAccountNumber,
                BankName = seller.BankName,
                AccountHolderName = seller.AccountHolderName,
                BankBranch = seller.BankBranch,
                QrcodeImage = seller.QrcodeImage
            };
        }

        //GET: Lấy Seller hiện tại theo UserId (người đang đăng nhập)
        public async Task<SellerResponse?> GetCurrentSellerAsync(int userId)
        {
            var seller = await _sellerRepo.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy thông tin người bán tương ứng với tài khoản này.");

            return new SellerResponse
            {
                SellerId = seller.SellerId,
                UserId = seller.UserId,
                CompanyName = seller.CompanyName,
                Address = seller.Address,
                BankAccountNumber = seller.BankAccountNumber,
                BankName = seller.BankName,
                AccountHolderName = seller.AccountHolderName,
                BankBranch = seller.BankBranch,
                QrcodeImage = seller.QrcodeImage
            };
        }

        public async Task<bool> UpdateSellerAsync(int userId, UpdateSellerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Tìm seller tương ứng với user đang đăng nhập
            var seller = await _sellerRepo.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy người bán tương ứng với tài khoản này.");

            // Cập nhật thông tin text
            seller.CompanyName = request.CompanyName?.Trim() ?? seller.CompanyName;
            seller.Address = request.Address?.Trim() ?? seller.Address;
            seller.BankAccountNumber = request.BankAccountNumber?.Trim() ?? seller.BankAccountNumber;
            seller.BankName = request.BankName?.Trim() ?? seller.BankName;
            seller.AccountHolderName = request.AccountHolderName?.Trim() ?? seller.AccountHolderName;
            seller.BankBranch = request.BankBranch?.Trim() ?? seller.BankBranch;

            // Upload QR code mới (nếu có)
            if (request.QRCodeImage != null && request.QRCodeImage.Length > 0)
            {
                try
                {
                    // Nếu có QR code cũ thì xóa đi
                    if (!string.IsNullOrEmpty(seller.QrcodeImage))
                    {
                        var oldPublicId = _cloudinary.ExtractPublicId(seller.QrcodeImage);
                        if (!string.IsNullOrEmpty(oldPublicId))
                            await _cloudinary.DeleteAsync(oldPublicId);
                    }

                    // Upload ảnh mới
                    var uploadedUrl = await _cloudinary.UploadImageAsync(request.QRCodeImage, "ecowrap/sellers/qrcodes");
                    seller.QrcodeImage = uploadedUrl;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Lỗi khi tải ảnh QR code lên Cloudinary.", ex);
                }
            }

            await _sellerRepo.UpdateAsync(seller);
            await _sellerRepo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteSellerAsync(int id)
        {
            var seller = await _sellerRepo.GetBySellerIdAsync(id);
            if (seller == null)
                return false;

            //Lấy thông tin user (để có email)
            var user = await _userRepo.GetByIdAsync(seller.UserId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy người bán này.");

            //Xóa đơn đăng ký của user (nếu có)
            var registrationList = await _registrationRepo.GetByUserIdAsync(seller.UserId);
            if (registrationList != null && registrationList.Any())
            {
                foreach (var registration in registrationList)
                {
                    await _registrationRepo.DeleteAsync(registration);
                }
                await _registrationRepo.SaveChangesAsync();
            }

            //Xóa seller
            await _sellerRepo.DeleteAsync(seller);
            await _sellerRepo.SaveChangesAsync();

            //Gửi email thông báo
            string subject = "EcoWrap - Hủy quyền người bán";
            string body = $@"
                <p>Xin chào <b>{user.FullName ?? "người dùng"}</b>,</p>
                <p>Tài khoản của bạn đã bị <b>hủy quyền người bán</b> trên hệ thống EcoWrap.</p>
                <p>Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ với bộ phận hỗ trợ.</p>
                <p>Trân trọng,<br>Đội ngũ EcoWrap Support</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return true;
        }
    }
}