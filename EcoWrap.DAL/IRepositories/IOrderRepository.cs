using EcoWrap.DAL.Entities;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface IOrderRepository
    {
        Task AddAsync(Order entity);
        Task UpdateAsync(Order entity);
        Task<Order?> GetByIdAsync(int id);
        Task SaveChangesAsync();
        Task<IEnumerable<Order>> GetAllAdminAsync();
        Task<IEnumerable<Order>> GetAllByCustomerIdAsync(int customerId);
        Task<Order?> GetByIdWithDetailsAsync(int orderId);
        Task<Order?> GetByIdWithDetailsForAdminAsync(int orderId);
        Task<IEnumerable<Order>> GetAllByStatusAdminAsync(string status);
    }
}