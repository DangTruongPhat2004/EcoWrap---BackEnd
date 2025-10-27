using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IPaymentRepository
    {
        Task<int> CreatePaymentAsync(Payment payment);
        Task<bool> UpdatePaymentStatusAsync(string paymentLinkId, string newStatus, string? transactionCode);
        Task<Payment?> GetPaymentByLinkIdAsync(string paymentLinkId);
        Task SaveChangesAsync();
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    }
}