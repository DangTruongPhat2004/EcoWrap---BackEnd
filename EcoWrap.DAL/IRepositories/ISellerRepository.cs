using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface ISellerRepository
    {
        Task AddAsync(Seller seller);
        Task<Seller?> GetBySellerIdAsync(int id);
        Task<Seller?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Seller>> GetAllAsync();
        Task UpdateAsync(Seller seller);
        Task DeleteAsync(Seller entity);
        Task<bool> ExistsByUserIdAsync(int userId);
        Task<int> SaveChangesAsync();
    }
}