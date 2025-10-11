using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Request
{
    public class RegisterSellerRequest
    {
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhone { get; set; }
        public string LicenseId { get; set; }

        // File ảnh CMND/CCCD
        public IFormFile PersonalIdImage { get; set; }

        // File giấy phép kinh doanh
        public IFormFile BusinessLicenseFile { get; set; }
    }
}