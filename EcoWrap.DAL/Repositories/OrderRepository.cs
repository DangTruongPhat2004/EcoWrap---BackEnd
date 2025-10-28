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
        public async Task<IEnumerable<Order>> GetAllAdminAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.User)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }
        public async Task<IEnumerable<Order>> GetAllByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }
        public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.User)
                .Include(o => o.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p!.Images) // Include product images
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
        public async Task<Order?> GetByIdWithDetailsForAdminAsync(int orderId)
        {
            return await _context.Orders
               .Include(o => o.Customer)
                   .ThenInclude(c => c!.User) 
               .Include(o => o.CartItems) 
                   .ThenInclude(ci => ci.Product) 
                       .ThenInclude(p => p!.Seller) 
                           .ThenInclude(s => s!.User)
               .FirstOrDefaultAsync(o => o.OrderId == orderId); 
        }
        public async Task<IEnumerable<Order>> GetAllByStatusAdminAsync(string status)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.User) 
                .Where(o => EF.Functions.Like(o.OrderStatus, status))
                                                                    
                .OrderByDescending(o => o.CreatedDate) 
                .ToListAsync();
        }
    }
}