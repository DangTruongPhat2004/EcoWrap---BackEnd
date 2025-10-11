using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class SellerRepository : ISellerRepository
    {
        private readonly EcoWrapDbContext _context;

        public SellerRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Seller seller)
        {
            await _context.Sellers.AddAsync(seller);
        }

        public async Task<Seller?> GetByUserIdAsync(int userId)
        {
            return await _context.Sellers
                .Include(s => s.User) // load luôn thông tin user nếu có navigation property
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
        public async Task<Seller?> GetBySellerIdAsync(int id)
        {
            return await _context.Sellers
                .Include(s => s.User) // load thông tin User nếu có navigation property
                .FirstOrDefaultAsync(s => s.SellerId == id);
        }

        public async Task<IEnumerable<Seller>> GetAllAsync()
        {
            return await _context.Sellers
                .Include(s => s.User) // có thể bỏ nếu không cần thông tin user
                .ToListAsync();
        }

        public async Task UpdateAsync(Seller seller)
        {
            _context.Sellers.Update(seller);
        }

        public async Task DeleteAsync(Seller entity)
        {
            _context.Sellers.Remove(entity);
            await Task.CompletedTask;
        }
        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _context.Sellers.AnyAsync(a => a.UserId == userId);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}