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
            return null;
        }
    }
}