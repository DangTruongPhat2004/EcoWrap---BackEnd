using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EcoWrap.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly ICartRepository _cartRepo;
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ICustomerRepository _customerRepo;

        public OrderService(
            ICartRepository cartRepo,
            ICartItemRepository cartItemRepo,
            IOrderRepository orderRepo,
            ICustomerRepository customerRepo)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _orderRepo = orderRepo;
            _customerRepo = customerRepo;
        }

        public async Task<object> CreateOrderFromActiveCartAsync(int userId)
        {
            //Tìm customer
            var customer = await _customerRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin khách hàng.");

            //Lấy giỏ hàng đang hoạt động
            var cart = await _cartRepo.GetActiveCartByCustomerIdAsync(customer.CustomerId)
                ?? throw new InvalidOperationException("Không có giỏ hàng đang hoạt động.");

            //Lấy các CartItem
            var cartItems = await _cartItemRepo.GetByCartIdAsync(cart.CartId);
            if (cartItems == null || !cartItems.Any())
                throw new InvalidOperationException("Giỏ hàng trống, không thể tạo đơn hàng.");

            //Gom nhóm theo SellerId
            var groupedBySeller = cartItems.GroupBy(ci => ci.SellerId).ToList();

            var createdOrders = new List<object>();

            foreach (var group in groupedBySeller)
            {
                int sellerId = group.Key;
                var items = group.ToList();

                int totalAmount = items.Sum(i => i.Quantity);
                decimal totalPrice = items.Sum(i => i.CartItemPrice);

                //Tạo Order
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    SellerId = sellerId,
                    TotalAmount = totalAmount,
                    TotalPrice = totalPrice,
                    OrderStatus = "Pending",
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                await _orderRepo.AddAsync(order);
                await _orderRepo.SaveChangesAsync();

                //Cập nhật CartItem
                foreach (var item in items)
                {
                    item.OrderId = order.OrderId;
                    await _cartItemRepo.UpdateAsync(item);
                }

                await _cartItemRepo.SaveChangesAsync();

                createdOrders.Add(new
                {
                    order.OrderId,
                    order.SellerId,
                    order.TotalAmount,
                    order.TotalPrice,
                    order.OrderStatus
                });
            }

            //Khóa Cart
            cart.IsActive = false;
            await _cartRepo.UpdateAsync(cart);
            await _cartRepo.SaveChangesAsync();

            return new
            {
                Message = "Tạo đơn hàng thành công.",
                Data = createdOrders
            };
        }
    }
}