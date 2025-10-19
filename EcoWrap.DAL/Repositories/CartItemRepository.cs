using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories
{
    public class CartItemRepository : ICartItemRepository
    {
        private readonly EcoWrapDbContext _context;

        public CartItemRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<CartItem?> GetByIdAsync(int id)
        {
            return await _context.CartItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.CartItemId == id);
        }

        public async Task<CartItem?> GetByCartAndProductAsync(int cartId, string productId)
        {
            return await _context.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId);
        }

        public async Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId)
        {
            return await _context.CartItems
                .Include(i => i.Product)
                .Where(i => i.CartId == cartId)
                .ToListAsync();
        }

        public async Task AddAsync(CartItem entity)
        {
            await _context.CartItems.AddAsync(entity);
        }

        public async Task UpdateAsync(CartItem entity)
        {
            _context.CartItems.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(CartItem entity)
        {
            _context.CartItems.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteRangeAsync(IEnumerable<CartItem> entities)
        {
            _context.CartItems.RemoveRange(entities);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}