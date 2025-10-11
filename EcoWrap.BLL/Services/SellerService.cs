using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.Services
{
    public class SellerService : ISellerService
    {
        private readonly ISellerRepository _sellerRepo;
        private readonly IRegistrationRepository _registrationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;

        public SellerService(
            ISellerRepository sellerRepo,
            IRegistrationRepository registrationRepo,
            IUserRepository userRepo,
            IEmailService emailService)
        {
            _sellerRepo = sellerRepo ?? throw new ArgumentNullException(nameof(sellerRepo));
            _registrationRepo = registrationRepo ?? throw new ArgumentNullException(nameof(registrationRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<IEnumerable<Seller>> GetAllAsync()
        {
            return await _sellerRepo.GetAllAsync();
        }

        public async Task<Seller?> GetBySellerIdAsync(int id)
        {
            return await _sellerRepo.GetBySellerIdAsync(id);
        }

        public async Task<bool> UpdateSellerAsync(int id, UpdateSellerRequest request)
        {
            var seller = await _sellerRepo.GetBySellerIdAsync(id);
            if (seller == null)
                return false;

            seller.CompanyName = request.CompanyName?.Trim() ?? seller.CompanyName;
            seller.Address = request.Address?.Trim() ?? seller.Address;

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