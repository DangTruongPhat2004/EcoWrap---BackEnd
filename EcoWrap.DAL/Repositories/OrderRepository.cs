using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly EcoWrapDbContext _context;

        public OrderRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task AddAsync(Order entity)
        {
            await _context.Orders.AddAsync(entity);
        }
        public Task UpdateAsync(Order entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}