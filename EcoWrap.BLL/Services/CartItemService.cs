using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IProductRepository _productRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly ICartRepository _cartRepo;

        public CartItemService(
            ICartItemRepository cartItemRepo,
            IProductRepository productRepo,
            IInventoryRepository inventoryRepo,
            ICartRepository cartRepo)
        {
            _cartItemRepo = cartItemRepo;
            _productRepo = productRepo;
            _inventoryRepo = inventoryRepo;
            _cartRepo = cartRepo;
        }

        // -------------------- [PRIVATE METHOD - CHECK CART OWNERSHIP] --------------------
        private async Task<Cart> GetCartForUserAsync(int cartId, int userId)
        {
            var cart = await _cartRepo.GetByIdAsync(cartId)
                ?? throw new InvalidOperationException("Không tìm thấy giỏ hàng.");

            if (cart.CustomerId != userId)
                throw new InvalidOperationException("Bạn không có quyền thao tác trên giỏ hàng này.");

            return cart;
        }

        // -------------------- [CREATE CART ITEM] --------------------
        public async Task<object> CreateAsync(CreateCartItemRequest request, int userId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Dữ liệu gửi lên không hợp lệ.");

            if (request.Quantity <= 0)
                throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");

            // Lấy giỏ hàng active của user, nếu chưa có thì tạo mới
            var cart = await _cartRepo.GetActiveCartByCustomerIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = userId,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    CartTotalPrice = 0
                };
                await _cartRepo.AddAsync(cart);
                await _cartRepo.SaveChangesAsync(); // lưu để có CartId
            }

            // Kiểm tra sản phẩm tồn tại
            var product = await _productRepo.GetByIdAsync(request.ProductId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm trong hệ thống.");

            // Kiểm tra tồn kho
            var inventories = await _inventoryRepo.GetAllAsync();
            int totalStock = inventories
                .Where(i => i.ProductId == request.ProductId)
                .Sum(i => i.StockQuantity ?? 0);

            if (totalStock == 0)
                throw new InvalidOperationException("Sản phẩm hiện đã hết hàng.");

            if (request.Quantity > totalStock)
                throw new InvalidOperationException($"Chỉ còn {totalStock} sản phẩm trong kho, không thể thêm {request.Quantity}.");

            // Kiểm tra trùng sản phẩm trong giỏ
            var existingItem = await _cartItemRepo.GetByCartAndProductAsync(cart.CartId, request.ProductId);
            if (existingItem != null)
                throw new InvalidOperationException("Sản phẩm này đã có trong giỏ hàng. Hãy cập nhật số lượng thay vì thêm mới.");

            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CartItemPrice = request.Quantity * product.ProductPrice
            };

            await _cartItemRepo.AddAsync(cartItem);
            await _cartItemRepo.SaveChangesAsync();

            // Cập nhật tổng giá Cart
            await UpdateCartTotalPriceAsync(cart.CartId);

            return new
            {
                Message = "Đã thêm sản phẩm vào giỏ hàng thành công.",
                Data = new
                {
                    cartItem.CartItemId,
                    cartItem.CartId,
                    cartItem.ProductId,
                    ProductName = product.ProductName,
                    product.ProductPrice,
                    cartItem.Quantity,
                    cartItem.CartItemPrice
                }
            };
        }

        // -------------------- [UPDATE CART ITEM] --------------------
        public async Task<object> UpdateAsync(int cartItemId, UpdateCartItemRequest request, int userId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Dữ liệu gửi lên không hợp lệ.");

            if (request.Quantity <= 0)
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

            var cartItem = await _cartItemRepo.GetByIdAsync(cartItemId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm trong giỏ hàng.");

            // Kiểm tra quyền sở hữu cart
            await GetCartForUserAsync(cartItem.CartId, userId);

            var product = cartItem.Product ?? await _productRepo.GetByIdAsync(cartItem.ProductId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin sản phẩm.");

            // Kiểm tra tồn kho
            var inventories = await _inventoryRepo.GetAllAsync();
            int totalStock = inventories
                .Where(i => i.ProductId == cartItem.ProductId)
                .Sum(i => i.StockQuantity ?? 0);

            if (totalStock == 0)
                throw new InvalidOperationException("Sản phẩm hiện đã hết hàng.");

            if (request.Quantity > totalStock)
                throw new InvalidOperationException($"Chỉ còn {totalStock} sản phẩm trong kho, không thể đặt {request.Quantity}.");

            cartItem.Quantity = request.Quantity;
            cartItem.CartItemPrice = request.Quantity * product.ProductPrice;

            await _cartItemRepo.UpdateAsync(cartItem);
            await _cartItemRepo.SaveChangesAsync();

            // Cập nhật tổng giá Cart
            await UpdateCartTotalPriceAsync(cartItem.CartId);

            return new
            {
                Message = "Cập nhật giỏ hàng thành công.",
                Data = new
                {
                    cartItem.CartItemId,
                    cartItem.CartId,
                    cartItem.ProductId,
                    ProductName = product.ProductName,
                    product.ProductPrice,
                    cartItem.Quantity,
                    cartItem.CartItemPrice
                }
            };
        }

        // -------------------- [DELETE CART ITEM] --------------------
        public async Task<bool> DeleteAsync(int cartItemId, int userId)
        {
            var cartItem = await _cartItemRepo.GetByIdAsync(cartItemId);
            if (cartItem == null)
                throw new InvalidOperationException("Không tìm thấy sản phẩm trong giỏ hàng.");

            // Kiểm tra quyền sở hữu cart
            await GetCartForUserAsync(cartItem.CartId, userId);

            int cartId = cartItem.CartId;

            await _cartItemRepo.DeleteAsync(cartItem);
            await _cartItemRepo.SaveChangesAsync();

            // Cập nhật tổng giá Cart
            await UpdateCartTotalPriceAsync(cartId);

            return true;
        }

        // -------------------- [PRIVATE METHOD - UPDATE CART TOTAL PRICE] --------------------
        private async Task UpdateCartTotalPriceAsync(int cartId)
        {
            var cart = await _cartRepo.GetByIdAsync(cartId)
                ?? throw new InvalidOperationException("Không tìm thấy giỏ hàng để cập nhật tổng giá.");

            var cartItems = await _cartItemRepo.GetByCartIdAsync(cartId);
            cart.CartTotalPrice = cartItems.Sum(i => i.CartItemPrice);

            await _cartRepo.UpdateAsync(cart);
            await _cartRepo.SaveChangesAsync();
        }
    }
}