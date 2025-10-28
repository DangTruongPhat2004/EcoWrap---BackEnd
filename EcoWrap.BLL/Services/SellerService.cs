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
                CompanyImage = s.CompanyImage,
                CompanyBanner = s.CompanyBanner,
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
                CompanyImage = seller.CompanyImage,
                CompanyBanner = seller.CompanyBanner,
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
                CompanyImage = seller.CompanyImage,
                CompanyBanner = seller.CompanyBanner,
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

            // Find seller based on the logged-in user's ID
            var seller = await _sellerRepo.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy thông tin người bán tương ứng với tài khoản này.");

            bool changed = false; // Track if any changes were made

            // Update text fields (only if value provided and different)
            if (request.CompanyName != null && seller.CompanyName != request.CompanyName.Trim())
            {
                seller.CompanyName = request.CompanyName.Trim();
                changed = true;
            }
            if (request.Address != null && seller.Address != request.Address.Trim())
            {
                seller.Address = request.Address.Trim();
                changed = true;
            }
            if (request.BankAccountNumber != null && seller.BankAccountNumber != request.BankAccountNumber.Trim())
            {
                seller.BankAccountNumber = request.BankAccountNumber.Trim();
                changed = true;
            }
            if (request.BankName != null && seller.BankName != request.BankName.Trim())
            {
                seller.BankName = request.BankName.Trim();
                changed = true;
            }
            if (request.AccountHolderName != null && seller.AccountHolderName != request.AccountHolderName.Trim())
            {
                seller.AccountHolderName = request.AccountHolderName.Trim();
                changed = true;
            }
            if (request.BankBranch != null && seller.BankBranch != request.BankBranch.Trim())
            {
                seller.BankBranch = request.BankBranch.Trim();
                changed = true;
            }


            // --- Handle Image Uploads ---
            string? oldPublicId = null; // Variable to store old public ID for deletion

            // Upload QR code (if provided)
            if (request.QRCodeImage != null && request.QRCodeImage.Length > 0)
            {
                try
                {
                    // Attempt to delete old image if it exists
                    if (!string.IsNullOrEmpty(seller.QrcodeImage))
                    {
                        oldPublicId = _cloudinary.ExtractPublicId(seller.QrcodeImage); // Assume helper exists
                        // Delete handled after saving new URL below
                    }
                    // Upload new image
                    var uploadedUrl = await _cloudinary.UploadImageAsync(request.QRCodeImage, $"EcoWrap/Sellers/{seller.SellerId}/QRCode");
                    seller.QrcodeImage = uploadedUrl; // Update URL
                    changed = true;
                    // Delete old file *after* successfully uploading and getting new URL
                    if (!string.IsNullOrEmpty(oldPublicId))
                    {
                        await _cloudinary.DeleteAsync(oldPublicId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and decide whether to stop or continue
                    Console.WriteLine($"[Cloudinary Error] Failed QR Code upload for SellerId {seller.SellerId}: {ex.Message}");
                    throw new InvalidOperationException("Lỗi khi tải ảnh QR code.", ex); // Rethrow to inform client
                }
                oldPublicId = null; // Reset for next image
            }

            // Upload Company Image (if provided)
            if (request.CompanyImage != null && request.CompanyImage.Length > 0)
            {
                try
                {
                    if (!string.IsNullOrEmpty(seller.CompanyImage))
                    {
                        oldPublicId = _cloudinary.ExtractPublicId(seller.CompanyImage);
                    }
                    var uploadedUrl = await _cloudinary.UploadImageAsync(request.CompanyImage, $"EcoWrap/Sellers/{seller.SellerId}/CompanyImage");
                    seller.CompanyImage = uploadedUrl;
                    changed = true;
                    if (!string.IsNullOrEmpty(oldPublicId))
                    {
                        await _cloudinary.DeleteAsync(oldPublicId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cloudinary Error] Failed Company Image upload for SellerId {seller.SellerId}: {ex.Message}");
                    throw new InvalidOperationException("Lỗi khi tải ảnh công ty.", ex);
                }
                oldPublicId = null;
            }

            // Upload Company Banner (if provided)
            if (request.CompanyBanner != null && request.CompanyBanner.Length > 0)
            {
                try
                {
                    if (!string.IsNullOrEmpty(seller.CompanyBanner))
                    {
                        oldPublicId = _cloudinary.ExtractPublicId(seller.CompanyBanner);
                    }
                    var uploadedUrl = await _cloudinary.UploadImageAsync(request.CompanyBanner, $"EcoWrap/Sellers/{seller.SellerId}/CompanyBanner");
                    seller.CompanyBanner = uploadedUrl;
                    changed = true;
                    if (!string.IsNullOrEmpty(oldPublicId))
                    {
                        await _cloudinary.DeleteAsync(oldPublicId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cloudinary Error] Failed Company Banner upload for SellerId {seller.SellerId}: {ex.Message}");
                    throw new InvalidOperationException("Lỗi khi tải ảnh bìa công ty.", ex);
                }
            }

            // --- Save Changes (only if needed) ---
            if (changed)
            {
                // No need for _sellerRepo.UpdateAsync(seller) if using EF Core tracking
                await _sellerRepo.SaveChangesAsync();
                Console.WriteLine($"[SellerService] Seller profile updated for UserId {userId}.");
            }
            else
            {
                Console.WriteLine($"[SellerService] No changes detected for Seller profile update (UserId {userId}).");
            }

            return changed; // Return true if changes were saved, false otherwise
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