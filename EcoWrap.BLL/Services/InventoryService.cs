using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IProductRepository _productRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly ISellerRepository _sellerRepo;
        private readonly EcoWrapDbContext _dbContext;
        public InventoryService(IInventoryRepository inventoryRepo,
                                IProductRepository productRepo,
                                IWarehouseRepository warehouseRepo,
                                ISellerRepository sellerRepo,
                                EcoWrapDbContext dbContext)
        {
            _inventoryRepo = inventoryRepo;
            _productRepo = productRepo;
            _warehouseRepo = warehouseRepo;
            _sellerRepo = sellerRepo;
            _dbContext = dbContext;
        }

        // -------------------- [CREATE INVENTORY] --------------------
        public async Task<Inventory> CreateInventoryAsync(int userId, CreatInventoryRequest request)
        {
            if (request.StockQuantity < 0)
                throw new InvalidOperationException("StockQuantity không thể nhỏ hơn 0.");

            var product = await _productRepo.GetByIdAsync(request.ProductId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var warehouse = await _warehouseRepo.GetByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new InvalidOperationException("Warehouse không tồn tại.");

            var existingInventory = await _inventoryRepo.GetByProductAndWarehouseAsync(request.ProductId, request.WarehouseId);
            if (existingInventory != null)
                throw new InvalidOperationException("Inventory cho sản phẩm này và warehouse đã tồn tại.");

            var inventory = new Inventory
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                StockQuantity = request.StockQuantity,
                LastUpdated = DateTime.UtcNow.AddHours(7)
            };

            await _inventoryRepo.AddAsync(inventory);

            // Update Product status nếu StockQuantity > 0
            if (request.StockQuantity > 0 && product.ProductStatus != "In stock")
            {
                product.ProductStatus = "In stock";
                await _productRepo.UpdateAsync(product);
            }

            await _inventoryRepo.SaveChangesAsync();
            await _productRepo.SaveChangesAsync();

            return inventory;
        }

        // -------------------- [GET ALL] --------------------
        public async Task<IEnumerable<Inventory>> GetAllAsync()
        {
            return await _inventoryRepo.GetAllAsync();
        }

        // -------------------- [GET BY ID] --------------------
        public async Task<Inventory?> GetByIdAsync(int id)
        {
            return await _inventoryRepo.GetByIdAsync(id);
        }

        // -------------------- [GET MY INVENTORY] --------------------
        public async Task<IEnumerable<Inventory>> GetMyInventoryAsync(int userId)
        {
            // CẦN THIẾT: Hàm GetByUserIdAsync phải có trong ISellerRepository
            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Người dùng này không có vai trò Seller.");

            // Lấy danh sách ProductId thuộc về Seller này (Dùng hàm đã có: GetAllBySellerIdAsync)
            var sellerProducts = await _productRepo.GetAllBySellerIdAsync(seller.SellerId);
            var productIds = sellerProducts.Select(p => p.ProductId).ToList();

            if (!productIds.Any())
                return new List<Inventory>();

            // TỐI ƯU: Sử dụng hàm mới để lọc ở cấp độ Database
            // CẦN THIẾT: Hàm GetInventoriesByProductIdsAsync phải có trong IInventoryRepository
            var myInventories = await _inventoryRepo.GetInventoriesByProductIdsAsync(productIds);

            return myInventories;
        }

        // -------------------- [UPDATE INVENTORY] --------------------
        public async Task<Inventory> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request)
        {
            if (request.StockQuantity < 0)
                throw new InvalidOperationException("StockQuantity không thể nhỏ hơn 0.");

            var inventory = await _inventoryRepo.GetByIdAsync(inventoryId);
            if (inventory == null)
                throw new InvalidOperationException("Inventory không tồn tại.");

            inventory.StockQuantity = request.StockQuantity;
            inventory.LastUpdated = DateTime.UtcNow.AddHours(7);

            await _inventoryRepo.UpdateAsync(inventory);
            await _inventoryRepo.SaveChangesAsync();

            // Cập nhật Product status dựa trên tổng StockQuantity tất cả Inventory
            var inventoriesOfProduct = await _inventoryRepo.GetAllAsync();
            int totalStock = inventoriesOfProduct
                             .Where(i => i.ProductId == inventory.ProductId)
                             .Sum(i => i.StockQuantity ?? 0);

            var product = await _productRepo.GetByIdAsync(inventory.ProductId);
            if (product != null)
            {
                string newStatus = totalStock > 0 ? "In stock" : "Out stock";
                if (product.ProductStatus != newStatus)
                {
                    product.ProductStatus = newStatus;
                    await _productRepo.UpdateAsync(product);
                    await _productRepo.SaveChangesAsync();
                }
            }

            return inventory;
        }

        // -------------------- [DELETE INVENTORY] --------------------
        public async Task<bool> DeleteInventoryAsync(int inventoryId)
        {
            var inventory = await _inventoryRepo.GetByIdAsync(inventoryId);
            if (inventory == null) return false;

            string productId = inventory.ProductId;

            // Xóa Inventory
            await _inventoryRepo.DeleteAsync(inventory);
            await _inventoryRepo.SaveChangesAsync();

            // Kiểm tra tổng StockQuantity của product này còn bao nhiêu
            var inventoriesOfProduct = await _inventoryRepo.GetAllAsync();
            int totalStock = 0;
            foreach (var inv in inventoriesOfProduct)
            {
                if (inv.ProductId == productId)
                    totalStock += inv.StockQuantity ?? 0;
            }

            // Nếu tổng stock = 0 thì update Product status
            if (totalStock == 0)
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product != null && product.ProductStatus != "Out stock")
                {
                    product.ProductStatus = "Out stock";
                    await _productRepo.UpdateAsync(product);
                    await _productRepo.SaveChangesAsync();
                }
            }
            return true;
        }
    }
}