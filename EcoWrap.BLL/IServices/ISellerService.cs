using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.IServices
{
    public interface ISellerService
    {
        Task<IEnumerable<Seller>> GetAllAsync();
        Task<Seller?> GetBySellerIdAsync(int id);
        Task<bool> UpdateSellerAsync(int id, UpdateSellerRequest request);
        Task<bool> DeleteSellerAsync(int id);
    }
}