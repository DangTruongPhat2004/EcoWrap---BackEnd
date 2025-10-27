using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Request
{
    public class UpdateProductRequest
    {
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

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal ProductPrice { get; set; }

        [Required(ErrorMessage = "Cân nặng (gram) là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Cân nặng phải là số dương")]
        public int WeightGram { get; set; }

        // Upload files (lưu URL lên DB)
        public IFormFile? ImageFile { get; set; }
        public IFormFile? CertificateFile { get; set; }
        public IFormFile? DesignFile { get; set; }
    }
}
