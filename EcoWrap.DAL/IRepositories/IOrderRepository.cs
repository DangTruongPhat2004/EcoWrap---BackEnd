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
    }
}