using EcoWrap.DAL.Entities;
using EcoWrap.Models.DTO;
using EcoWrap.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IProductService
    {
        Task<bool> CreateProductAsync(int userId, CreateProductRequest request);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string id);
        Task<bool> DeleteAsync(string id, int userId);
        Task<IEnumerable<Product>> GetProductsBySellerAsync(int userId);
        Task<bool> UpdateProductAsync(string productId, int userId, UpdateProductRequest request);
        Task<ProductDetailDto> GetProductDetailAsync(string productId);
        Task<bool> ActivateProductAsync(string productId, int userId);
        Task<bool> DeactivateProductAsync(string productId, int userId);
    }
}