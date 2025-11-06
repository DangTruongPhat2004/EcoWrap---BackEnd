// File: DAL/Repositories/InventoryRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Ensure Linq is included

namespace EcoWrap.DAL.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly EcoWrapDbContext _context;

        public InventoryRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        // --- Existing methods ---
        public async Task AddAsync(Inventory inventory)
        {
            await _context.Inventories.AddAsync(inventory);
        }

        public async Task<Inventory?> GetByIdAsync(int id)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .FirstOrDefaultAsync(i => i.InventoryId == id);
        }

        public async Task<IEnumerable<Inventory>> GetAllAsync()
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .ToListAsync();
        }

        public async Task UpdateAsync(Inventory inventory)
        {
            _context.Inventories.Update(inventory);
            // Consider returning Task.CompletedTask for async consistency if not using await _context.SaveChangesAsync() here
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Inventory inventory)
        {
            _context.Inventories.Remove(inventory);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Inventory?> GetByProductAndWarehouseAsync(string productId, int warehouseId)
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
        }

        // --- Updated methods ---

        public async Task<bool> DecreaseStockAcrossWarehousesAsync(string productId, int quantityToDecrease)
        {
            if (quantityToDecrease <= 0) return true;

            // 1. Find inventories, use StockQuantity
            var inventories = await _context.Inventories
                                      .Where(i => i.ProductId == productId && i.StockQuantity > 0) // Use StockQuantity
                                      .OrderBy(i => i.WarehouseId)
                                      .ToListAsync();

            if (!inventories.Any())
            {
                Console.WriteLine($"[Inventory Error] No stock found for Product {productId} in any warehouse.");
                return false;
            }

            int remainingQuantityToDecrease = quantityToDecrease;

            // 2. Iterate and deduct stock, use StockQuantity
            foreach (var inventory in inventories)
            {
                if (remainingQuantityToDecrease <= 0) break;

                // Use StockQuantity
                // File: DAL/Repositories/InventoryRepository.cs (Dòng 93)
                int quantityToTakeFromThisWarehouse = Math.Min(inventory.StockQuantity.GetValueOrDefault(0), remainingQuantityToDecrease);

                // Use StockQuantity
                inventory.StockQuantity -= quantityToTakeFromThisWarehouse;
                inventory.LastUpdated = DateTime.UtcNow.AddHours(7);
                _context.Inventories.Update(inventory);

                remainingQuantityToDecrease -= quantityToTakeFromThisWarehouse;
            }

            // 3. Check if enough stock was found, use StockQuantity
            if (remainingQuantityToDecrease > 0)
            {
                // Use StockQuantity for logging if needed
                Console.WriteLine($"[Inventory Error] Insufficient total stock for Product {productId}. Short by {remainingQuantityToDecrease}.");
                return false;
            }

            return true;
        }

        public async Task IncreaseStockInFirstWarehouseAsync(string productId, int quantityToIncrease)
        {
            if (quantityToIncrease <= 0) return;

            // 1. Find the first inventory record
            var inventory = await _context.Inventories
                                      .Where(i => i.ProductId == productId)
                                      .OrderBy(i => i.WarehouseId)
                                      .FirstOrDefaultAsync();

            if (inventory == null)
            {
                Console.WriteLine($"[Inventory Error] Cannot increase stock. Inventory record not found for Product {productId} in any warehouse.");
                return;
            }

            // 2. Increase stock, use StockQuantity
            inventory.StockQuantity += quantityToIncrease; // Use StockQuantity
            inventory.LastUpdated = DateTime.UtcNow.AddHours(7);
            _context.Inventories.Update(inventory);
        }

        public async Task<IEnumerable<Inventory>> GetInventoriesByProductIdsAsync(IEnumerable<string> productIds)
        {
            // Kiểm tra list ProductIds nếu rỗng để tránh truy vấn không cần thiết
            if (productIds == null || !productIds.Any())
            {
                return Enumerable.Empty<Inventory>();
            }

            // Sử dụng .Where(i => productIds.Contains(i.ProductId)) 
            // Entity Framework sẽ dịch thành SQL: WHERE ProductId IN ('P001', 'P002', ...)
            return await _context.Inventories
                .Where(i => productIds.Contains(i.ProductId))
                // Nên include Warehouse nếu bạn muốn hiển thị tên kho/địa chỉ trong chi tiết tồn kho
                .Include(i => i.Warehouse)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}