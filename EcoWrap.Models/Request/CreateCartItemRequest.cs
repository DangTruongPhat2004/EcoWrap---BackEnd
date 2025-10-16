using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Request
{
    public class CreateCartItemRequest
    {
        public string ProductId { get; set; } = null!;
        public int Quantity { get; set; }
    }
}