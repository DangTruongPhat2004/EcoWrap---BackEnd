using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IUserRepository _userRepo;
        private readonly EcoWrapDbContext _dbContext;

        public AdminService(
            IAdminRepository adminRepo,
            IUserRepository userRepo,
            EcoWrapDbContext dbContext)
        {
            _adminRepo = adminRepo ?? throw new ArgumentNullException(nameof(adminRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<Admin>> GetAllAdminsAsync()
        {
            return await _adminRepo.GetAllAsync();
        }

        public async Task<Admin?> GetAdminByIdAsync(int adminId)
        {
            if (adminId <= 0)
                throw new ArgumentException("ID admin không hợp lệ.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                throw new InvalidOperationException("Không tìm thấy admin.");

            return admin;
        }

        public async Task<string> CreateAdminAsync(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId không hợp lệ.");

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy người dùng có UserId này.");

            // Kiểm tra xem user đã là admin chưa
            var allAdmins = await _adminRepo.GetAllAsync();
            foreach (var a in allAdmins)
            {
                if (a.UserId == userId)
                    throw new InvalidOperationException("Người dùng này đã là admin, không thể thêm lại.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật RoleId cho User = 1 (Admin)
                user.RoleId = 1;
                await _userRepo.UpdateAsync(user);
                await _userRepo.SaveChangesAsync();

                // Tạo bản ghi Admin tương ứng
                var admin = new Admin { UserId = userId };
                await _adminRepo.AddAsync(admin);
                await _adminRepo.SaveChangesAsync();

                await transaction.CommitAsync();

                return $"Đã thêm userId {userId} thành admin (RoleId = 1) thành công.";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> UpdateAdminAsync(int adminId, int userId)
        {
            if (adminId <= 0 || userId <= 0)
                throw new ArgumentException("AdminId hoặc UserId không hợp lệ.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                throw new InvalidOperationException("Không tìm thấy admin cần cập nhật.");

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy người dùng.");

            //Kiểm tra xem user mới có phải đã là admin chưa
            var allAdmins = await _adminRepo.GetAllAsync();
            foreach (var a in allAdmins)
            {
                if (a.UserId == userId && a.AdminId != adminId)
                    throw new InvalidOperationException("Người dùng này đã là admin khác, không thể gán trùng.");
            }

            admin.UserId = userId;
            await _adminRepo.UpdateAsync(admin);
            await _adminRepo.SaveChangesAsync();

            return $"Cập nhật admin (ID: {adminId}) với userId {userId} thành công.";
        }

        public async Task<string> DeleteAdminAsync(int adminId)
        {
            if (adminId <= 0)
                throw new ArgumentException("AdminId không hợp lệ.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                throw new InvalidOperationException("Không tìm thấy admin để xóa.");

            await _adminRepo.DeleteAsync(admin);
            await _adminRepo.SaveChangesAsync();

            return $"Đã xóa admin (ID: {adminId}) thành công.";
        }
    }
}