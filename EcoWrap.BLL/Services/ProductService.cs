using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.DTO;
using EcoWrap.Models.Request;
// Bạn có thể cần thêm namespace của CloudinaryService
// using EcoWrap.BLL.Services.ThirdParty; 

namespace EcoWrap.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ISellerRepository _sellerRepo;
        private readonly CloudinaryService _cloudinary; // Giả sử CloudinaryService đã được inject

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

                // [SỬA ĐỔI] Thêm WeightGram
                WeightGram = request.WeightGram,

                ProductStatus = "Out stock", // Mặc định là hết hàng, cần nhập kho
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

        public async Task<IEnumerable<Product>> GetProductsBySellerIdAsync(int sellerId)
        {
            var sellerExists = await _sellerRepo.GetBySellerIdAsync(sellerId);
            if (sellerExists == null)
                throw new InvalidOperationException($"Không tìm thấy người bán với ID: {sellerId}.");
            return await _productRepo.GetAllBySellerIdAsync(sellerId);
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
            product.WeightGram = request.WeightGram;

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
        // [SỬA ĐỔI] Thêm userId để xác thực
        public async Task<bool> DeleteAsync(string id, int userId)
        {
            // [SỬA ĐỔI] Thêm logic xác thực
            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy người bán.");

            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                throw new InvalidOperationException("Không tìm thấy sản phẩm.");

            if (product.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền xoá sản phẩm này.");

            // (Thêm logic kiểm tra xem sản phẩm có trong đơn hàng nào chưa)
            // ...

            await _productRepo.DeleteAsync(product);
            await _productRepo.SaveChangesAsync();
            return true;
        }

        public async Task<ProductDetailDto> GetProductDetailAsync(string productId)
        {
            // Sử dụng Repo để lấy Product Entity
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm có ID: {productId}.");

            if (product.WeightGram == null || product.WeightGram <= 0)
                throw new InvalidOperationException($"Sản phẩm {product.ProductName} chưa có thông tin trọng lượng (WeightGram) hợp lệ.");

            // Ánh xạ từ Product Entity sang DTO
            return new ProductDetailDto
            {
                ProductName = product.ProductName,
                // Dùng toán tử Null-coalescing để đảm bảo giá trị (mặc dù đã kiểm tra ở trên)
                WeightGram = product.WeightGram.Value
            };
        }

        // -------------------- [ACTIVATE PRODUCT] --------------------
        public async Task<bool> ActivateProductAsync(string productId, int userId)
        {
            Console.WriteLine($"[ProductService] Attempting to activate ProductId: {productId} by UserId: {userId}");
            // 1. Xác thực Seller
            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin người bán.");

            // 2. Lấy Product
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm ID: {productId}.");

            // 3. Kiểm tra quyền sở hữu
            if (product.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái sản phẩm này.");

            // 4. Kiểm tra trạng thái hiện tại (nếu đã active thì không cần làm gì)
            if (product.IsActive)
            {
                Console.WriteLine($"[ProductService] Product {productId} is already active.");
                return true; // Hoặc trả về false với message "Sản phẩm đã được kích hoạt"
            }

            // 5. Cập nhật IsActive = true
            product.IsActive = true;
            // Không cần gọi UpdateAsync nếu repo dùng DbContext tracking
            // await _productRepo.UpdateAsync(product);
            await _productRepo.SaveChangesAsync();
            Console.WriteLine($"[ProductService] Product {productId} activated successfully.");
            return true;
        }

        // -------------------- [DEACTIVATE PRODUCT] --------------------
        public async Task<bool> DeactivateProductAsync(string productId, int userId)
        {
            Console.WriteLine($"[ProductService] Attempting to deactivate ProductId: {productId} by UserId: {userId}");
            // 1. Xác thực Seller
            var seller = await _sellerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin người bán.");

            // 2. Lấy Product
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm ID: {productId}.");

            // 3. Kiểm tra quyền sở hữu
            if (product.SellerId != seller.SellerId)
                throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái sản phẩm này.");

            // 4. Kiểm tra trạng thái hiện tại (nếu đã inactive thì không cần làm gì)
            if (!product.IsActive)
            {
                Console.WriteLine($"[ProductService] Product {productId} is already inactive.");
                return true; // Hoặc trả về false với message "Sản phẩm đã ngừng kích hoạt"
            }

            // 5. Cập nhật IsActive = false
            product.IsActive = false;
            // await _productRepo.UpdateAsync(product);
            await _productRepo.SaveChangesAsync();
            Console.WriteLine($"[ProductService] Product {productId} deactivated successfully.");
            return true;
        }
    }
}