using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly EcoWrapDbContext _context;

        public CustomerRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<Customer?> GetByUserIdAsync(int userId)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}