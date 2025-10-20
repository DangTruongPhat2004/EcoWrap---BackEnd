using EcoWrap.DAL.Entities;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
    }
}