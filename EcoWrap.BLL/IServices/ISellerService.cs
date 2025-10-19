using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;

namespace EcoWrap.BLL.IServices
{
    public interface ISellerService
    {
        Task<IEnumerable<SellerResponse>> GetAllAsync();
        Task<SellerResponse?> GetCurrentSellerAsync(int userId);
        Task<SellerResponse?> GetBySellerIdAsync(int id);
        Task<bool> UpdateSellerAsync(int id, UpdateSellerRequest request);
        Task<bool> DeleteSellerAsync(int id);
    }
}