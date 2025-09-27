using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Request
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; }
        public string VerificationCode { get; set; } = null!;
    }
}