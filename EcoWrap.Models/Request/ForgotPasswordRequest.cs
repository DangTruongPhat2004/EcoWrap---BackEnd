using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Request
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }
}