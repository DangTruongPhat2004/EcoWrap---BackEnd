using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Payment
{
    public class CreatePaymentResponse
    {
        public bool Success { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? QrCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}