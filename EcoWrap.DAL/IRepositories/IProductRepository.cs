using EcoWrap.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string productId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task SaveChangesAsync();
        Task<IEnumerable<Product>> GetAllBySellerIdAsync(int sellerId);
        Task<bool> ProductIdExistsAsync(string productId);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<IEnumerable<Product>> GetActiveAsync();
    }
}