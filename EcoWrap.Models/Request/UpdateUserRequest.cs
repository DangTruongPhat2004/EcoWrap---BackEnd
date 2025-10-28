using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Request
{
    public class UpdateUserRequest
    {
        public string? Username { get; set; } 
        public string? Email { get; set; }
        public string? FullName { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}