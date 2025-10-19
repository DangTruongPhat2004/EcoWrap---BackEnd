using EcoWrap.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.DAL.IRepositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        Task<int> SaveChangesAsync();
    }
}