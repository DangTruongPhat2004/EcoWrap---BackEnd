using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepo;

        public CategoryService(ICategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _categoryRepo.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _categoryRepo.GetByIdAsync(id);
        }

        public async Task<bool> CreateAsync(CreateCategoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Kiểm tra trùng ID
            var existing = await _categoryRepo.GetByIdAsync(request.CategoryId);
            if (existing != null)
                throw new InvalidOperationException($"Category với ID '{request.CategoryId}' đã tồn tại.");

            // Kiểm tra trùng tên
            var all = await _categoryRepo.GetAllAsync();
            if (all.Any(c => string.Equals(c.CategoryName.Trim(), request.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category với tên '{request.CategoryName}' đã tồn tại.");

            var category = new Category
            {
                CategoryId = request.CategoryId,
                CategoryName = request.CategoryName.Trim()
            };

            await _categoryRepo.AddAsync(category);
            await _categoryRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(UpdateCategoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!request.CategoryId.HasValue)
                throw new InvalidOperationException("Cần cung cấp CategoryId để cập nhật.");

            var category = await _categoryRepo.GetByIdAsync(request.CategoryId.Value);
            if (category == null)
                throw new InvalidOperationException($"Category với ID '{request.CategoryId}' không tồn tại.");

            // Kiểm tra trùng tên (nếu có tên mới)
            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                var all = await _categoryRepo.GetAllAsync();
                bool nameExists = all.Any(c =>
                    c.CategoryId != request.CategoryId.Value &&
                    string.Equals(c.CategoryName.Trim(), request.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                    throw new InvalidOperationException($"Tên Category '{request.CategoryName}' đã tồn tại.");
            }

            // Cập nhật tên
            category.CategoryName = request.CategoryName?.Trim() ?? category.CategoryName;

            await _categoryRepo.UpdateAsync(category);
            await _categoryRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null)
                return false;

            await _categoryRepo.DeleteAsync(category);
            await _categoryRepo.SaveChangesAsync();
            return true;
        }
    }
}