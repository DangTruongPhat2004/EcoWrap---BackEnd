using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
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

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}