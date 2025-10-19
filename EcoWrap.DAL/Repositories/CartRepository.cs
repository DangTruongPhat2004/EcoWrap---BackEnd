using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly EcoWrapDbContext _context;

        public CartRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cart>> GetAllAsync()
        {
            return await _context.Carts.ToListAsync();
        }

        public async Task<Cart?> GetByIdAsync(int id)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CartId == id);
        }

        public async Task<Cart?> GetActiveCartByCustomerIdAsync(int customerId)
        {
            return await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive == true);
        }

        public async Task<IEnumerable<Cart>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task AddAsync(Cart entity)
        {
            await _context.Carts.AddAsync(entity);
        }

        public async Task UpdateAsync(Cart entity)
        {
            _context.Carts.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Cart entity)
        {
            _context.Carts.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}