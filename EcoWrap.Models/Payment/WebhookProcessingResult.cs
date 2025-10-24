using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Payment
{
    public class WebhookProcessingResult
    {
        public bool IsValidSignature { get; set; } // Chữ ký có hợp lệ không?
        public bool IsSuccess { get; set; } // Giao dịch có thành công không (dựa vào data)?
        public int OrderId { get; set; } // ID đơn hàng từ webhook data
        public string? TransactionCode { get; set; } // Mã giao dịch PayOS
        public string? ErrorMessage { get; set; }
    }
}