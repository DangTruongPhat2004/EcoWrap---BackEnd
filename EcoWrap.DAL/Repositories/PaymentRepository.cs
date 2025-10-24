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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly EcoWrapDbContext _context;

        public PaymentRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreatePaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment.PaymentId;
        }

        public async Task<bool> UpdatePaymentStatusAsync(string paymentLinkId, string newStatus, string? transactionCode)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentLinkId == paymentLinkId);

            if (payment == null)
            {
                return false;
            }

            payment.PaymentStatus = newStatus;
            payment.TransactionCode = transactionCode;

            // Nếu bạn muốn cập nhật thời gian
            // payment.UpdatedDate = DateTime.UtcNow; 

            await _context.SaveChangesAsync();
            return true;
        }

        public Task<Payment?> GetPaymentByLinkIdAsync(string paymentLinkId)
        {
            return _context.Payments
               .FirstOrDefaultAsync(p => p.PaymentLinkId == paymentLinkId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}