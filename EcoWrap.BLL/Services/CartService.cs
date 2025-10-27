using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly ICartItemRepository _cartItemRepo;
        private readonly ICustomerRepository _customerRepo;

        public CartService(
            ICartRepository cartRepo,
            ICartItemRepository cartItemRepo,
            ICustomerRepository customerRepo)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _customerRepo = customerRepo;
        }

        // -------------------- [CREATE CART] --------------------
        public async Task<object> CreateCartAsync(int userId)
        {
            var customer = await _customerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin khách hàng cho tài khoản này.");

            var existingCart = await _cartRepo.GetActiveCartByCustomerIdAsync(customer.CustomerId);
            if (existingCart != null)
                throw new InvalidOperationException("Bạn đã có giỏ hàng đang hoạt động.");

            var newCart = new Cart
            {
                CustomerId = customer.CustomerId,
                CreatedDate = DateTime.Now,
                IsActive = true,
                CartTotalPrice = decimal.Zero
            };

            await _cartRepo.AddAsync(newCart);
            await _cartRepo.SaveChangesAsync();

            return new
            {
                Message = "Tạo giỏ hàng thành công.",
                Data = new
                {
                    newCart.CartId,
                    newCart.CustomerId,
                    newCart.CreatedDate,
                    newCart.IsActive,
                    newCart.CartTotalPrice
                }
            };
        }

        // -------------------- [GET ALL CARTS OF CURRENT USER] --------------------
        public async Task<object> GetAllCartsAsync(int userId)
        {
            var customer = await _customerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin khách hàng.");

            var carts = await _cartRepo.GetByCustomerIdAsync(customer.CustomerId);
            carts = carts.Where(c => c.IsActive == true).ToList();

            if (carts == null || !carts.Any())
                throw new InvalidOperationException("Bạn chưa có giỏ hàng nào.");

            var result = new List<object>();

            foreach (var cart in carts)
            {
                // Lấy danh sách cart items trong giỏ
                var cartItems = await _cartItemRepo.GetByCartIdAsync(cart.CartId);
                decimal totalPrice = cartItems.Sum(i => i.CartItemPrice);

                result.Add(new
                {
                    cart.CartId,
                    cart.CustomerId,
                    cart.CreatedDate,
                    cart.IsActive,
                    CartTotalPrice = totalPrice,

                    // Thêm danh sách CartItems vào JSON
                    Items = cartItems.Select(i => new
                    {
                        i.CartItemId,
                        i.ProductId,
                        i.Quantity,
                        i.CartItemPrice
                    })
                });
            }

            return new
            {
                Message = "Lấy danh sách giỏ hàng thành công.",
                Data = result
            };
        }

        // -------------------- [GET CART BY ID] --------------------
        public async Task<object> GetCartByIdAsync(int userId, int cartId)
        {
            var customer = await _customerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin khách hàng.");

            var cart = await _cartRepo.GetByIdAsync(cartId)
                ?? throw new InvalidOperationException("Không tìm thấy giỏ hàng.");

            if (cart.CustomerId != customer.CustomerId)
                throw new InvalidOperationException("Bạn không có quyền truy cập giỏ hàng này.");

            var cartItems = await _cartItemRepo.GetByCartIdAsync(cart.CartId);
            decimal totalPrice = cartItems.Sum(i => i.CartItemPrice);

            return new
            {
                Message = "Lấy thông tin giỏ hàng thành công.",
                Data = new
                {
                    cart.CartId,
                    cart.CustomerId,
                    cart.CreatedDate,
                    cart.IsActive,
                    CartTotalPrice = totalPrice,
                    Items = cartItems.Select(i => new
                    {
                        i.CartItemId,
                        i.ProductId,
                        ProductName = i.Product?.ProductName,
                        i.Quantity,
                        i.CartItemPrice
                    })
                }
            };
        }

        // -------------------- [DELETE CART] --------------------
        public async Task<object> DeleteCartAsync(int userId, int cartId)
        {
            var customer = await _customerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin khách hàng.");

            var cart = await _cartRepo.GetByIdAsync(cartId)
                ?? throw new InvalidOperationException("Không tìm thấy giỏ hàng.");

            if (cart.CustomerId != customer.CustomerId)
                throw new InvalidOperationException("Bạn không có quyền xóa giỏ hàng này.");

            var cartItems = await _cartItemRepo.GetByCartIdAsync(cart.CartId);
            foreach (var item in cartItems)
                await _cartItemRepo.DeleteAsync(item);

            //await _cartItemRepo.SaveChangesAsync();

            await _cartRepo.DeleteAsync(cart);
            await _cartRepo.SaveChangesAsync();

            return new { Message = "Xóa giỏ hàng thành công." };
        }
    }
}