using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using Google.Apis.Drive.v3.Data;

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

        //Lấy toàn bộ kho
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _warehouseRepository.GetAllAsync();
        }

        //Lấy kho theo ID
        public async Task<Warehouse?> GetByIdAsync(int id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null)
                throw new InvalidOperationException("Không tìm thấy kho hàng.");

            return warehouse;
        }

        //Tạo kho mới (chỉ cho Seller có tài khoản)
        public async Task<bool> CreateWarehouseAsync(int userId, CreateWarehouseRequest request)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Tài khoản này không thuộc người bán nào.");

            var newWarehouse = new Warehouse
            {
                WarehouseName = request.WarehouseName,
                Address = request.Address,
                SellerId = seller.SellerId
            };

            await _warehouseRepository.AddAsync(newWarehouse);
            await _warehouseRepository.SaveChangesAsync();
            return true;
        }

        //Cập nhật kho (phải là kho của seller đó)
        public async Task<bool> UpdateWarehouseAsync(int warehouseId, int userId, UpdateWarehouseRequest request)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy thông tin người bán.");

            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);
            if (warehouse == null)
                throw new InvalidOperationException("Không tìm thấy kho để cập nhật.");

            if (warehouse.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật kho này.");

            warehouse.WarehouseName = request.WarehouseName;
            warehouse.Address = request.Address;

            await _warehouseRepository.UpdateAsync(warehouse);
            await _warehouseRepository.SaveChangesAsync();

            return true;
        }

        //Lấy danh sách kho của người bán hiện tại
        public async Task<IEnumerable<Warehouse>> GetWarehousesBySellerAsync(int userId)
        {
            var seller = await _sellerRepository.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Không tìm thấy người bán.");

            return await _warehouseRepository.GetAllBySellerIdAsync(seller.SellerId);
        }

        //Xoá kho (chỉ khi seller sở hữu kho đó)
        public async Task<bool> DeleteAsync(int id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null)
                throw new InvalidOperationException("Không tìm thấy kho để xoá.");

            var seller = await _sellerRepository.GetBySellerIdAsync(id);
            if (warehouse.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền xoá kho này.");

            await _warehouseRepository.DeleteAsync(warehouse);
            await _warehouseRepository.SaveChangesAsync();

            return true;
        }
    }
}