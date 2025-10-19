using EcoWrap.DAL.Entities;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);
        Task SaveChangesAsync();
    }
}