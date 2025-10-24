using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Response
{
    public class GhtkResponse
    {
        public bool Success { get; set; }
        public string TransportUnit { get; set; } // "GHTK"
        public string ShippingCode { get; set; } // Mã vận đơn
        public string Message { get; set; }
    }
}