using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ISellerRepository _sellerRepo;
        private readonly CloudinaryService _cloudinary;

        public ProductService(
            IProductRepository productRepo,
            ISellerRepository sellerRepo,
            CloudinaryService cloudinary)
        {
            _productRepo = productRepo;
            _sellerRepo = sellerRepo;
            _cloudinary = cloudinary;
        }

        // -------------------- [CREATE PRODUCT] --------------------
        public async Task<bool> CreateProductAsync(int userId, CreateProductRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var seller = await _sellerRepo.GetByUserIdAsync(userId);
            if (seller == null)
                throw new InvalidOperationException("Người dùng không phải là người bán hợp lệ.");

            var productIdExist = await _productRepo.ProductIdExistsAsync(request.ProductId);
            if (productIdExist)
                throw new InvalidOperationException("ProductId đã tồn tại.");

            var categoryExists = await _productRepo.CategoryExistsAsync(request.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException("Category không tồn tại.");

            string? imageUrl = null;
            string? certUrl = null;
            string? designUrl = null;

            try
            {
                if (request.ImageFile != null)
                    imageUrl = await _cloudinary.UploadFileAsync(request.ImageFile, "EcoWrap/Products/Images");

                if (request.CertificateFile != null)
                    certUrl = await _cloudinary.UploadFileAsync(request.CertificateFile, "EcoWrap/Products/Certificates");

                if (request.DesignFile != null)
                    designUrl = await _cloudinary.UploadFileAsync(request.DesignFile, "EcoWrap/Products/Designs");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi tải file lên Cloudinary.", ex);
            }

            var product = new Product
            {
                ProductId = request.ProductId,
                SellerId = seller.SellerId,
                CategoryId = request.CategoryId,
                MaterialType = request.MaterialType,
                ProductName = request.ProductName,
                ProductDescription = request.ProductDescription,
                Unit = request.Unit,
                Size = request.Size,
                InkType = request.InkType,
                PrintProcess = request.PrintProcess,
                ProductPrice = request.ProductPrice,
                ProductStatus = "Out stock",
                ImageFile = imageUrl,
                CertificateFile = certUrl,
                DesignFile = designUrl,
                CreatedDate = DateTime.UtcNow
            };

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();

            return true;
        }

        // -------------------- [GET] --------------------
        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _productRepo.GetAllAsync();

        public async Task<Product?> GetByIdAsync(string id)
            => await _productRepo.GetByIdAsync(id);

        public async Task<IEnumerable<Product>> GetProductsBySellerAsync(int userId)
        {
            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy người bán.");
            return await _productRepo.GetAllBySellerIdAsync(seller.SellerId);
        }

        // -------------------- [UPDATE] --------------------
        public async Task<bool> UpdateProductAsync(string productId, int userId, UpdateProductRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy người bán.");

            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

            if (product.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật sản phẩm này.");

            product.ProductName = request.ProductName;
            product.ProductDescription = request.ProductDescription;
            product.ProductPrice = request.ProductPrice;
            product.MaterialType = request.MaterialType;
            product.Unit = request.Unit;
            product.Size = request.Size;
            product.InkType = request.InkType;
            product.PrintProcess = request.PrintProcess;
            product.CategoryId = request.CategoryId;

            try
            {
                if (request.ImageFile != null)
                    product.ImageFile = await _cloudinary.UploadFileAsync(request.ImageFile, "EcoWrap/Products/Images");

                if (request.CertificateFile != null)
                    product.CertificateFile = await _cloudinary.UploadFileAsync(request.CertificateFile, "EcoWrap/Products/Certificates");

                if (request.DesignFile != null)
                    product.DesignFile = await _cloudinary.UploadFileAsync(request.DesignFile, "EcoWrap/Products/Designs");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi tải file lên Cloudinary.", ex);
            }

            await _productRepo.UpdateAsync(product);
            await _productRepo.SaveChangesAsync();
            return true;
        }

        // -------------------- [DELETE] --------------------
        public async Task<bool> DeleteAsync(string id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return false;

            await _productRepo.DeleteAsync(product);
            await _productRepo.SaveChangesAsync();
            return true;
        }
    }
}