using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly EcoWrapDbContext _context;

        public ProductRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Product>> GetAllBySellerIdAsync(int sellerId)
        {
            return await _context.Products
                .Where(w => w.SellerId == sellerId)
                .ToListAsync();
        }
        public async Task<bool> ProductIdExistsAsync(string productId)
            => await _context.Products.AnyAsync(p => p.ProductId == productId);
        public async Task<bool> CategoryExistsAsync(int categoryId)
            => await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
    }
}