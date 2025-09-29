using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface ICustomerRepository
    {
        Task AddAsync(Customer customer);
        Task<Customer?> GetByUserIdAsync(int userId);
    }
}