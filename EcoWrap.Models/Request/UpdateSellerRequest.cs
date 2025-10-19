using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Request
{
    public class UpdateSellerRequest
    {
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BankBranch { get; set; }
        public IFormFile QRCodeImage { get; set; }
    }
}