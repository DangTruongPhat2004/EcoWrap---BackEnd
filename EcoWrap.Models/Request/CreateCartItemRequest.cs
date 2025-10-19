using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Request
{
    public class CreateCartItemRequest
    {
        public string ProductId { get; set; } = null!;
        public int Quantity { get; set; }
        public IFormFile? Design_file { get; set; }
    }
}