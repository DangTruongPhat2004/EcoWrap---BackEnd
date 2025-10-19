using EcoWrap.DAL.Entities;
using EcoWrap.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<bool> CreateAsync(CreateCategoryRequest request);
        Task<bool> UpdateAsync(UpdateCategoryRequest request);
        Task<bool> DeleteAsync(int id);
    }
}