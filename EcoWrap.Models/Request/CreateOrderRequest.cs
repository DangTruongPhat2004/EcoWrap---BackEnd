using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Request
{
    public class CreateOrderRequest
    {
        public string Address { get; set; } = null!;
    }
}