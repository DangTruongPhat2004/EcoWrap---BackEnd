using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request; // Cần chắc chắn các Request Model được tạo
using System.Linq; // Cần dùng cho FirstOrDefaultAsync nếu IWarehouseRepository trả về IQueryable

namespace EcoWrap.BLL.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseService(ISellerRepository sellerRepository, IWarehouseRepository warehouseRepository)
        {
            _sellerRepository = sellerRepository;
            _warehouseRepository = warehouseRepository;
        }

        // Lấy toàn bộ kho
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _warehouseRepository.GetAllAsync();
        }

        // Lấy kho theo ID
        public async Task<Warehouse?> GetByIdAsync(int id)
        {
            // Không cần throw exception ở đây, để Controller xử lý NotFound
            return await _warehouseRepository.GetByIdAsync(id);
        }

        // Tạo kho mới (chỉ cho Seller có tài khoản)
        public async Task<bool> CreateWarehouseAsync(int userId, CreateWarehouseRequest request)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Tài khoản này không thuộc người bán nào.");

            var newWarehouse = new Warehouse
            {
                WarehouseName = request.WarehouseName,
                Address = request.Address,
                SellerId = seller.SellerId,

                // <<< BỔ SUNG TRƯỜNG PHONE_NUMBER >>>
                PhoneNumber = request.PhoneNumber,
                // <<< HẾT BỔ SUNG >>>

                // Các trường ID địa chỉ (đã là string trong Entity mới)
                ProvinceId = request.ProvinceId,
                DistrictId = request.DistrictId,
                WardId = request.WardId
            };

            await _warehouseRepository.AddAsync(newWarehouse);
            await _warehouseRepository.SaveChangesAsync();
            return true;
        }

        // Cập nhật kho (phải là kho của seller đó)
        public async Task<bool> UpdateWarehouseAsync(int warehouseId, int userId, UpdateWarehouseRequest request)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy thông tin người bán.");

            // Tối ưu: Lấy kho kèm theo thông tin Seller nếu có thể (để tránh 2 query)
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);

            if (warehouse == null)
                throw new InvalidOperationException("Không tìm thấy kho để cập nhật.");

            if (warehouse.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật kho này.");

            // Cập nhật tất cả các trường
            warehouse.WarehouseName = request.WarehouseName;
            warehouse.Address = request.Address;

            // <<< BỔ SUNG TRƯỜNG PHONE_NUMBER >>>
            warehouse.PhoneNumber = request.PhoneNumber;
            // <<< HẾT BỔ SUNG >>>

            warehouse.ProvinceId = request.ProvinceId;
            warehouse.DistrictId = request.DistrictId;
            warehouse.WardId = request.WardId;

            await _warehouseRepository.UpdateAsync(warehouse);
            await _warehouseRepository.SaveChangesAsync();

            return true;
        }

        // Lấy danh sách kho của người bán hiện tại
        public async Task<IEnumerable<Warehouse>> GetWarehousesBySellerAsync(int userId)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                // Trả về danh sách rỗng nếu không phải seller, hoặc ném lỗi nếu muốn ngăn chặn
                throw new InvalidOperationException("Không tìm thấy người bán.");

            // Hàm này giả định đã có trong IWarehouseRepository
            return await _warehouseRepository.GetAllBySellerIdAsync(seller.SellerId);
        }

        // Xoá kho (chỉ khi seller sở hữu kho đó)
        public async Task<bool> DeleteAsync(int warehouseId, int userId)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy thông tin người bán.");

            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);
            if (warehouse == null)
                throw new InvalidOperationException("Không tìm thấy kho để xoá.");

            if (warehouse.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền xoá kho này.");

            await _warehouseRepository.DeleteAsync(warehouse);
            await _warehouseRepository.SaveChangesAsync();

            return true;
        }
    }
}