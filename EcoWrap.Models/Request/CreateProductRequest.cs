using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace EcoWrap.Models.Request
{
    public class CreateProductRequest
    {
        [Required]
        public string ProductId { get; set; } = null!;
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public string ProductName { get; set; } = null!;
        public string? ProductDescription { get; set; }
        public string? MaterialType { get; set; }
        public string? Unit { get; set; }
        public string? Size { get; set; }
        public string? InkType { get; set; }
        public string? PrintProcess { get; set; }
        [Required]
        public decimal ProductPrice { get; set; }

        // Upload files (lưu URL lên DB)
        public IFormFile? ImageFile { get; set; }
        public IFormFile? CertificateFile { get; set; }
        public IFormFile? DesignFile { get; set; }
    }
}