using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly EcoWrapDbContext _context;

        public WarehouseRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses
                .Include(w => w.Seller)
                .ToListAsync();
        }

        public async Task<Warehouse?> GetByIdAsync(int id)
        {
            return await _context.Warehouses
                .Include(w => w.Seller)
                .FirstOrDefaultAsync(w => w.WarehouseId == id);
        }

        public async Task<IEnumerable<Warehouse>> GetAllBySellerIdAsync(int sellerId)
        {
            return await _context.Warehouses
                .Where(w => w.SellerId == sellerId)
                .ToListAsync();
        }

        public async Task AddAsync(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
        }

        public async Task UpdateAsync(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Warehouse warehouse)
        {
            _context.Warehouses.Remove(warehouse);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}